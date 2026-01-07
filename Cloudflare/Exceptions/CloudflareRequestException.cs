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

public class CloudflareRequestException : Exception
{
    public CloudflareRequestException(string message) : base($"An error occurred sending request to Cloudflare: {message}")
    {
    }

    public CloudflareRequestException(Exception ex) : base("An error occurred sending request to Cloudflare", ex)
    {
        
    }
}
