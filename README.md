SecureSign
==========

SecureSign is a simple code signing server. It is designed to allow Authenticode signing of build artifacts in a secure way, without exposing the private key.

Features
========
 - **Isolation**: Each unique call site has its own access token for the service. An access token only provides access to a single signing key, and can be restricted to only sign particular content.
 - **Encryption**: Private keys are encrypted at rest, using an encryption key that is not stored on the server. The encryption key is encoded into the access token, so the private key can not be decrypted without the access token.
 - **Cross-platform**: Supported on Windows and Linux, may run on other operating systems too.

Prerequisites
=============
 - [ASP.NET Core 2.0 runtime](https://www.microsoft.com/net/download/core#/runtime) must be installed
 - On Linux, [osslsigncode](https://sourceforge.net/projects/osslsigncode/) needs to be installed. On Debian-based distros, you can `apt-get install osslsigncode`.
 - On Windows, [signtool](https://docs.microsoft.com/en-us/dotnet/framework/tools/signtool-exe) needs to be installed. This comes with the Windows SDK.

Usage
=====
Before using SecureSign, you need to add your private signing keys. Ensure you have the key as a `.pfx` file, and then use the `addkey` command:

```
$ dotnet SecureSign.Tools.dll addkey /tmp/my_key.pfx
Password? **********

Saved my_key.pfx (Test code signing)
Subject: CN=Test Code Signing
Issuer: CN=Test Code Signing
Valid from 2016-11-20 1:51:47 PM until 2017-12-31 4:00:00 PM

Secret Code: 2eiONi53ihUkxxewk5VliJO29hLM3S68LHkTkt9aKuX4dgRop99zw
```

This secret code is the decryption key required to create access tokens that use this key. It is not saved anywhere, so if you lose it you will need to reinstall the key.

Once the key has been added, create an access token for it using the `addtoken` command:
```
$ dotnet SecureSign.Tools.dll addtoken
Key name? my_key.pfx
Secret code? 2eiONi53ihUkxxewk5VliJO29hLM3S68LHkTkt9aKuX4dgRop99zw
Comment (optional)? Hello World

Signing settings:
Description? Some App
Product/Application URL? https://dan.cx/SecureSignSample/

Created new access token:
zGZEtxVisE2rqSf71SVqNg-CfDJ8BqN7wE-jn1Hj20Xn2jQTrtxA6zDlrQn0C3Ut....
```

Some of the access token information is saved into `accessTokenConfig.json`. Removing the access token from this config file will revoke its access.

Now that an access token has been created, you can start the server:
```
$ ASPNETCORE_ENVIRONMENT=Production ASPNETCORE_URLS=http://*:5000/ dotnet SecureSign.Web.dll
Hosting environment: Production
Content root path: /var/www/securesign
Now listening on: http://[::]:5000
Application started. Press Ctrl+C to shut down.
```

To sign a file, send a POST request to `/sign/authenticode`. This can  contain a URL to the artifact to sign:
```
curl --show-error --fail \
  -X POST \
  -F 'accessToken=zGZEtxVis...' \
  -F 'artifactUrl=https://build.example.com/latest.msi' \
  -o latest-signed.msi \
  http://localhost:5000/sign/authenticode
```

Alternatively, it could contain the actual artifact itself:
```
curl --show-error --fail \
  -X POST \
  -F 'accessToken=zGZEtxVis...' \
  -F 'artifact=@latest.msi' \
  -o latest-signed.msi \
  http://localhost:5000/sign/authenticode
```

HTTPS
-----
To use TLS, first obtain a certificate in .pfx format. Then, pass in the certificate file name and password as environment variables:
```
$ ASPNETCORE_ENVIRONMENT=Production LISTEN_IP=* HTTPS_PORT=12345 HTTPS_CERT=test.pfx HTTPS_CERT_PASSWORD=password1 dotnet SecureSign.Web.dll
```

Restricting Usage of Access Tokens
----------------------------------

By default, an access token can be used to sign **any** file, either via upload or from **any** URL. To lock this down, you can modify `accessTokenConfig.json`. For example, to only allow `msi` files from `nightly.yarnpkg.com` to be signed, you can set `AllowUploads` to `false` and add the whitelist to `AllowedUrls`:
```
{
  "AccessTokens": {
    "zGZEtxVisE2rqSf71SVqNg": {
      "AllowUploads": false,
      "AllowedUrls": [
      {
        "Domain": "^nightly.yarnpkg.com$",
        "Path": "^/(.+)\.msi$"
      }
    ]
  }
}
```

Each item in the `AllowedUrls` array contains regular expressions to check the `Domain` and `Path` of the URL. The request will be allowed if any of the allowed URL patterns match.
