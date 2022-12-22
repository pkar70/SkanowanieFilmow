/*
 * Developer: Ramtin Jokar [ Ramtinak@live.com ] [ License: MIT ]
 * 
 * 2022 - Dedicated Library
 */

using Newtonsoft.Json;
using System.Collections.Generic;

namespace FacebookApiSharp.Classes.Responses
{
    public class FacebookFailureLoginResponse
    {
        [JsonProperty("error")]
        public FacebookFailureLoginErrorResponse Error { get; set; }
    }

    public class FacebookFailureLoginErrorResponse
    {
        [JsonProperty("message")]
        public string Message { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("code")]
        public int Code { get; set; }
        [JsonProperty("error_data")]
        public FacebookFailureLoginErrorDataResponse ErrorData { get; set; }
        [JsonProperty("error_subcode")]
        public int ErrorSubcode { get; set; }
        [JsonProperty("is_transient")]
        public bool IsTransient { get; set; }
        [JsonProperty("error_user_title")]
        public string ErrorUserTitle { get; set; }
        [JsonProperty("error_user_msg")]
        public string ErrorUserMsg { get; set; }
        [JsonProperty("fbtrace_id")]
        public string FbtraceId { get; set; }
    }

    public class FacebookFailureLoginErrorDataResponse
    {
        [JsonProperty("pwd_enc_key_pkg")]
        public FacebookPwdEncKeyPkgResponse PwdEncKeyPkg { get; set; }
        [JsonProperty("error_subcode")]
        public int ErrorSubcode { get; set; }
        [JsonProperty("cpl_info")]
        public FacebookFailureLoginErrorDataCplInfoResponse CplInfo { get; set; }

        // PKAR, dla 406 (SMS code)
        public long uid { get; set; }
        public string login_first_factor { get; set; }
        public string auth_token { get; set; }
    }

    public class FacebookPwdEncKeyPkgResponse
    {
        [JsonProperty("key_id")]
        public int KeyId { get; set; }
        [JsonProperty("public_key")]
        public string PublicKey { get; set; }
        [JsonProperty("seconds_to_live")]
        public int SecondsToLive { get; set; }
    }
    public class FacebookFailureLoginErrorDataCplInfoResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("profile_pic_uri")]
        public string ProfilePicUri { get; set; }
        [JsonProperty("cpl_eligible")]
        public bool CplEligible { get; set; }
        [JsonProperty("cpl_after_openid")]
        //public Contactpoints contactpoints { get; set; }
        public bool CplAfterOpenid { get; set; }
        [JsonProperty("cpl_group")]
        public int CplGroup { get; set; }
        [JsonProperty("first_name")]
        public string FirstName { get; set; }
        [JsonProperty("password_reset_nonce_length")]
        public int PasswordResetNonceLength { get; set; }
        [JsonProperty("cpl_sms_retriever_auto_submit_test_group")]
        public string CplSmsRetrieverAutoSubmitTestGroup { get; set; }
        [JsonProperty("nonce_send_status")]
        public int NonceSendStatus { get; set; }
        [JsonProperty("show_dbl_cpl_interstitial")]
        public bool ShowDblCplInterstitial { get; set; }
    }


    public class FacebookAlbumPagedList
    {
        public List<FacebookAlbum> data { get; set; }
        public FacebookAlbumsListPaging paging { get; set; }
    }

    public class FacebookAlbumsListPaging
    {
        public FacebookAlbumsListPagingCursors cursors { get; set; }
        public string next { get; set; }
    }

    public class FacebookAlbumsListPagingCursors
    {
        public string before { get; set; }
        public string after { get; set; }  
    }

    public class FacebookAlbum
    {
        public string id { get; set; }
        public bool can_upload { get; set; }
        public int count { get; set; }
        public System.DateTime created_time { get; set; }
        public FacebookAlbumFrom from { get; set; }
        public string link { get; set; }
        public string name { get; set; }
        public string privacy { get; set; }
        public string type { get; set; }
        public System.DateTime updated_time { get; set; }
    }

    public class FacebookAlbumFrom
    {
        public string name { get; set; }
        public string id { get; set; }
    }

    //public class Contactpoints
    //{
    //    public _0 _0 { get; set; }
    //}

    //public class _0
    //{
    //    public string id { get; set; }
    //    public string display { get; set; }
    //    public string type { get; set; }
    //}

}
