//   Copyright (c) .NET Foundation and Contributors
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License. 

namespace RestSharp.Serializers.NewtonsoftJson;

[PublicAPI]
public static class RestClientExtensions {
    /// <summary>
    /// Use Newtonsoft.Json serializer with default settings
    /// </summary>
    /// <param name="client"></param>
    /// <returns></returns>
    public static RestClient UseNewtonsoftJson(this RestClient client) => client.UseSerializer(() => new JsonNetSerializer());

    /// <summary>
    /// Use Newtonsoft.Json serializer with custom settings
    /// </summary>
    /// <param name="client"></param>
    /// <param name="settings">Newtonsoft.Json serializer settings</param>
    /// <returns></returns>
    public static RestClient UseNewtonsoftJson(this RestClient client, JsonSerializerSettings settings)
        => client.UseSerializer(() => new JsonNetSerializer(settings));
}