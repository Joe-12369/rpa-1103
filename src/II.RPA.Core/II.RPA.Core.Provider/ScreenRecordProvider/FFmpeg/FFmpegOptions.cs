using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Provider.ScreenRecordProvider.FFmpeg
{
    public class FFmpegOptions
    {
        // General
        public bool OverrideCLIPath { get; set; } = false;
        public string CLIPath { get; set; } = "";
        public string VideoSource { get; set; } = FFmpegCLIManager.SourceGDIGrab;
        public string AudioSource { get; set; } = FFmpegCLIManager.SourceNone;
        public FFmpegVideoCodec VideoCodec { get; set; } = FFmpegVideoCodec.libx264;
        public FFmpegAudioCodec AudioCodec { get; set; } = FFmpegAudioCodec.libvoaacenc;
        public string UserArgs { get; set; } = "";
        public bool UseCustomCommands { get; set; } = false;
        public string CustomCommands { get; set; } = "";

        // Video
        public FFmpegPreset x264_Preset { get; set; } = FFmpegPreset.ultrafast;
        public int x264_CRF { get; set; } = 28;
        public int VPx_bitrate { get; set; } = 3000; // kbit/s
        public int XviD_qscale { get; set; } = 10;
        public FFmpegNVENCPreset NVENC_preset { get; set; } = FFmpegNVENCPreset.llhp;
        public int NVENC_bitrate { get; set; } = 3000; // kbit/s
        public FFmpegPaletteGenStatsMode GIFStatsMode { get; set; } = FFmpegPaletteGenStatsMode.full;
        public FFmpegPaletteUseDither GIFDither { get; set; } = FFmpegPaletteUseDither.sierra2_4a;
        public int GIFBayerScale { get; set; } = 2;
        public FFmpegAMFUsage AMF_usage { get; set; } = FFmpegAMFUsage.transcoding;
        public FFmpegAMFQuality AMF_quality { get; set; } = FFmpegAMFQuality.speed;
        public FFmpegQSVPreset QSV_preset { get; set; } = FFmpegQSVPreset.fast;
        public int QSV_bitrate { get; set; } = 3000;

        // Audio
        public int AAC_bitrate { get; set; } = 128; // kbit/s
        public int Opus_bitrate { get; set; } = 128; //kbit/s
        public int Vorbis_qscale { get; set; } = 3;
        public int MP3_qscale { get; set; } = 4;

        public string FFmpegPath => string.IsNullOrWhiteSpace(CLIPath) ? null : Path.GetFullPath(CLIPath);

        public string Extension
        {
            get
            {
                if (!VideoSource.Equals(FFmpegCLIManager.SourceNone, StringComparison.InvariantCultureIgnoreCase))
                {
                    switch (VideoCodec)
                    {
                        case FFmpegVideoCodec.libx264:
                        case FFmpegVideoCodec.libx265:
                        case FFmpegVideoCodec.h264_nvenc:
                        case FFmpegVideoCodec.hevc_nvenc:
                        case FFmpegVideoCodec.h264_amf:
                        case FFmpegVideoCodec.hevc_amf:
                        case FFmpegVideoCodec.h264_qsv:
                        case FFmpegVideoCodec.hevc_qsv:
                            return "mp4";
                        case FFmpegVideoCodec.libvpx:
                        case FFmpegVideoCodec.libvpx_vp9:
                            return "webm";
                        case FFmpegVideoCodec.libxvid:
                            return "avi";
                        case FFmpegVideoCodec.gif:
                            return "gif";
                        case FFmpegVideoCodec.libwebp:
                            return "webp";
                        case FFmpegVideoCodec.apng:
                            return "apng";
                    }
                }
                else if (!AudioSource.Equals(FFmpegCLIManager.SourceNone, StringComparison.InvariantCultureIgnoreCase))
                {
                    switch (AudioCodec)
                    {
                        case FFmpegAudioCodec.libvoaacenc:
                            return "m4a";
                        case FFmpegAudioCodec.libopus:
                            return "opus";
                        case FFmpegAudioCodec.libvorbis:
                            return "ogg";
                        case FFmpegAudioCodec.libmp3lame:
                            return "mp3";
                    }
                }

                return "mp4";
            }
        }

        public bool IsSourceSelected => IsVideoSourceSelected || IsAudioSourceSelected;

        public bool IsVideoSourceSelected => !string.IsNullOrEmpty(VideoSource) && !VideoSource.Equals(FFmpegCLIManager.SourceNone, StringComparison.InvariantCultureIgnoreCase);

        public bool IsAudioSourceSelected => !string.IsNullOrEmpty(AudioSource) && !AudioSource.Equals(FFmpegCLIManager.SourceNone, StringComparison.InvariantCultureIgnoreCase) &&
            (!IsVideoSourceSelected || !IsAnimatedImage);

        public bool IsAnimatedImage => VideoCodec == FFmpegVideoCodec.gif || VideoCodec == FFmpegVideoCodec.libwebp || VideoCodec == FFmpegVideoCodec.apng;

        public bool IsEvenSizeRequired => !IsAnimatedImage;

        public bool IsFullWindow { get; set; } = false;
        public bool IsFullSize { get; set; } = false;
    }
}
