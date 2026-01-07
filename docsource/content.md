## Overview

The Cloudflare Edge Orchestrator Extension is an integration that can inventory [Cloudflare Edge certificates](https://developers.cloudflare.com/ssl/concepts/#edge-certificate).

## Requirements

To inventory certificates, the Universal Orchestrator instance **must** be able to reach the server hosting the certificate over the network.  
Cloudflare's API does not provide the certificate contents, so the extension fetches the certificate directly from the server using a TLS handshake (via OpenSSL).

### Authentication and Authorization

When configuring the certificate store, the server password will contain the API token used to access the Cloudflare API. This can be either an **Account API Token** or a **User API Token**. See the [guide on how to create an API token](https://developers.cloudflare.com/fundamentals/api/get-started/create-token/).

The API token must have the following permissions:

|Permission|Required|Documentation|
|--|--|--|
|Account:SSL|Yes|[List Certificate Packs](https://developers.cloudflare.com/api/resources/ssl/subresources/certificate_packs/methods/list/)|
|Certificates:Read|Yes|[List Certificate Packs](https://developers.cloudflare.com/api/resources/ssl/subresources/certificate_packs/methods/list/)|

## Configuring the Certificate Store

When setting up your certificate store, you will be required to fill in the following properties on your certificate store:

|Name|Value|
|--|--|
|Client Machine|Account ID|
|Store Path|Zone ID|
|Server Username|<not used, can be set to No Value or an arbitrary value>|
|Server Password|Either your Account API Token or User API Token|

You can find your account ID and zone ID on the Cloudflare dashboard, located under your Account. Please refer to the [Cloudflare documentation](https://developers.cloudflare.com/fundamentals/account/find-account-and-zone-ids/) for more information.

<img src="./docsource/cloudflare-account-id-zone-id.png" width="400" height="300" alt="Account and Zone ID"> 