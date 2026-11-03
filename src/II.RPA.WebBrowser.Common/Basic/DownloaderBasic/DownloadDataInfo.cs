using Newtonsoft.Json;
using System;

namespace II.RPA.WebBrowser.Common.Basic.DownloaderBasic
{
    public class DownloadDataInfo
    {
        public IntPtr Handle { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("bytesReceived")]
        public long BytesReceived { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("canResume")]
        public string CanResume { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("danger")]
        public string Danger { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("estimatedEndTime")]
        public string EstimatedEndTime { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("exists")]
        public bool Exists { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("fileSize")]
        public long FileSize { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("filename")]
        public string Filename { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("finalUrl ")]
        public string FinalUrl { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("id")]
        public int Id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("incognito")]
        public string Incognito { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("mime")]
        public string Mime { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("paused")]
        public string Paused { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("referrer")]
        public string Referrer { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("startTime")]
        public string StartTime { get; set; }
        /// <summary>
        /// Indicates whether the download is progressing, interrupted, or complete.
        /// The interrupt state is not processed
        /// </summary>
        [JsonProperty("state")]
        public string State { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("totalBytes")]
        public long TotalBytes { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("url")]
        public string Url { get; set; }
    }

}
