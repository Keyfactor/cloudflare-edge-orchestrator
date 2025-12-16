## Overview

The Cloudflare Edge Orchestrator Extension is an integration that can inventory [Cloudflare Edge certificates](https://developers.cloudflare.com/ssl/concepts/#edge-certificate).

> IMPORTANT: In order for certificates to be inventoried, the Universal Orchestrator **must** be able to reach the server that hosts the certificate. Cloudflare's API does not return the certificate contents, so the Universal Orchestrator must pull the certificate from the server directly.

### Authentication and Authorization

When configuring the certificate store, the server password can be either an account API token or a user API token ([guide on how to create an API token](https://developers.cloudflare.com/fundamentals/api/get-started/create-token/)).

In order to read certificate pack information from the API, the API token must have at least `Account:SSL and Certificates:Read` privileges ([List Certificate Packs](https://developers.cloudflare.com/api/resources/ssl/subresources/certificate_packs/methods/list/)).