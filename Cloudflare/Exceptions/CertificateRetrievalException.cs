// Copyright 2026 Keyfactor
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Cloudflare.Exceptions;

public class CertificateRetrievalException : Exception
{
    public CertificateRetrievalException(string hostname) : base("Failed to retrieve the server certificate for host: " + hostname)
    {
    }

    public CertificateRetrievalException(string host, Exception innerException): base($"An error occurred while retrieving the server certificate for host {host}", innerException)
    {
        
    }
}
