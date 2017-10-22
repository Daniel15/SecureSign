SecureSign
==========

SecureSign is a simple code signing server. It is designed to allow Authenticode signing of build artifacts in a secure way, without exposing the private key.

Features
========
 - **Isolation**: Each unique call site has its own access token for the service. An access token only provides access to a single signing key.
 - **Encryption**: Private keys are encrypted at rest, using an encryption key that is not stored on the server. The encryption key is encoded into the access token.
 - **Cross-platform**: Supported on Windows and Linux, may run on other operating systems too.

Prerequisites
=============
 - [ASP.NET Core 2.0 runtime](https://www.microsoft.com/net/download/core#/runtime) must be installed
 - On Linux, [osslsigncode](https://sourceforge.net/projects/osslsigncode/) needs to be installed. On Debian-based distros, you can `apt-get install osslsigncode`.
 - On Windows, [signtool](https://docs.microsoft.com/en-us/dotnet/framework/tools/signtool-exe) needs to be installed. This comes with the Windows SDK.

Usage
=====
Add your private signing key as a `.pfx` file, using the `addkey` command. This will provide you with a secret code:

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

To sign a file, send a POST request to `/sign/authenticode`, containing the access token as well as a URL to a file to sign:
```
curl
  -H "Content-Type: application/json"
  -X POST
  -d '{"accessToken": "zGZEtxVis...", "artifactUrl": "https://build.example.com/latest.msi"}'
  http://localhost:5000/sign/authenticode > latest-signed.msi
```
