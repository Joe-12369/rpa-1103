using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenCapture
{
    public enum VideoLevel
    {
        None = 0,
        Low = 1,
        Middle = 2,
        High = 3
    }

    public class ScreencastOptions
    {
        public bool IsRecording { get; set; }
        public bool IsLossless { get; set; }
        public string InputPath { get; set; }
        public string OutputPath { get; set; }
        public int FPS { get; set; } = 30;
        public Rectangle CaptureArea { get; set; }
        /// <summary>
        /// 持续
        /// </summary>
        public float Duration { get; set; }
        public bool DrawCursor { get; set; }
        public VideoLevel Level { get; set; }
        public FFmpegOptions FFmpeg { get; set; } = new FFmpegOptions();

        public string GetFFmpegCommands()
        {
            string commands = GetFFmpegArgs();
            return commands.Trim();
        }

        public string GetFFmpegArgs(bool isCustom = false)
        {
            if (IsRecording && !FFmpeg.IsVideoSourceSelected && !FFmpeg.IsAudioSourceSelected)
            {
                return null;
            }

            StringBuilder args = new StringBuilder();
            args.Append("-threads 1 -rtbufsize 150M -probesize 150M "); // default real time buffer size was 3041280 (3M)

            string fps = FPS.ToString();

            if (IsRecording)
            {
                if (FFmpeg.IsVideoSourceSelected)
                {
                    if (FFmpeg.VideoSource.Equals(FFmpegCLIManager.SourceGDIGrab, StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (FFmpeg.IsFullWindow)
                        {
                            args.AppendFormat("-f gdigrab -framerate {0} -draw_mouse {1} -i desktop ",
                                fps, DrawCursor ? "1" : "0");
                        }
                        else
                        {
                            // http://ffmpeg.org/ffmpeg-devices.html#gdigrab
                            args.AppendFormat("-f gdigrab -framerate {0} -offset_x {1} -offset_y {2}  -draw_mouse {3} -video_size {4}x{5} -i desktop ",
                                fps, CaptureArea.X.ToString(), CaptureArea.Y.ToString(),
                                 DrawCursor ? "1" : "0", CaptureArea.Width, CaptureArea.Height);
                        }
                        //声音
                        if (FFmpeg.IsAudioSourceSelected)
                        {
                            args.AppendFormat("-f dshow -i audio={0} ", Helpers.EscapeCLIText(FFmpeg.AudioSource));
                        }
                    }
                    else
                    {
                        args.AppendFormat("-f dshow -framerate {0} -i video={1}", fps, Helpers.EscapeCLIText(FFmpeg.VideoSource));

                        if (FFmpeg.IsAudioSourceSelected)
                        {
                            args.AppendFormat(":audio={0} ", Helpers.EscapeCLIText(FFmpeg.AudioSource));
                        }
                        else
                        {
                            args.Append(" ");
                        }
                    }
                }
                else if (FFmpeg.IsAudioSourceSelected)
                {
                    args.AppendFormat("-f dshow -i audio={0} ", Helpers.EscapeCLIText(FFmpeg.AudioSource));
                }
            }
            else
            {
                args.Append($"-i \"{InputPath}\" ");
            }

            if (!string.IsNullOrEmpty(FFmpeg.UserArgs))
            {
                args.Append(FFmpeg.UserArgs + " ");
            }

            if (FFmpeg.IsVideoSourceSelected)
            {
                if (IsLossless || FFmpeg.VideoCodec != FFmpegVideoCodec.apng)
                {
                    string videoCodec;

                    if (IsLossless)
                    {
                        videoCodec = FFmpegVideoCodec.libx264.ToString();
                    }
                    else if (FFmpeg.VideoCodec == FFmpegVideoCodec.libvpx_vp9)
                    {
                        videoCodec = "libvpx-vp9";
                    }
                    else
                    {
                        videoCodec = FFmpeg.VideoCodec.ToString();
                    }

                    args.AppendFormat("-c:v {0} ", videoCodec);
                    args.AppendFormat("-r {0} ", fps); // output FPS
                }

                if (IsLossless)
                {
                    args.AppendFormat("-preset {0} ", FFmpegPreset.ultrafast);
                    args.AppendFormat("-tune {0} ", FFmpegTune.zerolatency);
                    args.AppendFormat("-qp {0} ", 0);
                }
                else
                {
                    switch (FFmpeg.VideoCodec)
                    {
                        case FFmpegVideoCodec.libx264: // https://trac.ffmpeg.org/wiki/Encode/H.264
                        case FFmpegVideoCodec.libx265: // https://trac.ffmpeg.org/wiki/Encode/H.265
                            args.AppendFormat("-preset {0} ", FFmpeg.x264_Preset);
                            if (IsRecording) args.AppendFormat("-tune {0} ", FFmpegTune.zerolatency);
                            args.AppendFormat("-crf {0} ", FFmpeg.x264_CRF);
                            args.AppendFormat("-pix_fmt {0} ", "yuv420p"); // -pix_fmt yuv420p required otherwise can't stream in Chrome
                            args.AppendFormat("-movflags {0} ", "+faststart"); // This will move some information to the beginning of your file and allow the video to begin playing before it is completely downloaded by the viewer
                            break;
                        case FFmpegVideoCodec.libvpx: // https://trac.ffmpeg.org/wiki/Encode/VP8
                        case FFmpegVideoCodec.libvpx_vp9: // https://trac.ffmpeg.org/wiki/Encode/VP9
                            if (IsRecording) args.AppendFormat("-deadline {0} ", "realtime");
                            args.AppendFormat("-b:v {0}k ", FFmpeg.VPx_bitrate);
                            args.AppendFormat("-pix_fmt {0} ", "yuv420p"); // -pix_fmt yuv420p required otherwise causing issues in Chrome related to WebM transparency support
                            break;
                        case FFmpegVideoCodec.libxvid: // https://trac.ffmpeg.org/wiki/Encode/MPEG-4
                            args.AppendFormat("-qscale:v {0} ", FFmpeg.XviD_qscale);
                            break;
                        case FFmpegVideoCodec.h264_nvenc: // https://trac.ffmpeg.org/wiki/HWAccelIntro#NVENC
                        case FFmpegVideoCodec.hevc_nvenc:
                            args.AppendFormat("-preset {0} ", FFmpeg.NVENC_preset);
                            args.AppendFormat("-b:v {0}k ", FFmpeg.NVENC_bitrate);
                            args.AppendFormat("-pix_fmt {0} ", "yuv420p");
                            break;
                        case FFmpegVideoCodec.h264_amf:
                        case FFmpegVideoCodec.hevc_amf:
                            args.AppendFormat("-usage {0} ", FFmpeg.AMF_usage);
                            args.AppendFormat("-quality {0} ", FFmpeg.AMF_quality);
                            args.AppendFormat("-pix_fmt {0} ", "yuv420p");
                            break;
                        case FFmpegVideoCodec.h264_qsv: // https://trac.ffmpeg.org/wiki/Hardware/QuickSync
                        case FFmpegVideoCodec.hevc_qsv:
                            args.AppendFormat("-preset {0} ", FFmpeg.QSV_preset);
                            args.AppendFormat("-b:v {0}k ", FFmpeg.QSV_bitrate);
                            break;
                        case FFmpegVideoCodec.libwebp: // https://www.ffmpeg.org/ffmpeg-codecs.html#libwebp
                            args.AppendFormat("-lossless {0} ", "0");
                            args.AppendFormat("-preset {0} ", "default");
                            args.AppendFormat("-loop {0} ", "0");
                            break;
                        case FFmpegVideoCodec.apng:
                            args.Append("-f apng ");
                            args.AppendFormat("-plays {0} ", "0");
                            break;
                    }
                }
            }

            if (FFmpeg.IsAudioSourceSelected)
            {
                switch (FFmpeg.AudioCodec)
                {
                    case FFmpegAudioCodec.libvoaacenc: // http://trac.ffmpeg.org/wiki/Encode/AAC
                        args.AppendFormat("-c:a aac -ac 2 -b:a {0}k ", FFmpeg.AAC_bitrate); // -ac 2 required otherwise failing with 7.1
                        break;
                    case FFmpegAudioCodec.libopus: // https://www.ffmpeg.org/ffmpeg-codecs.html#libopus-1
                        args.AppendFormat("-c:a libopus -b:a {0}k ", FFmpeg.Opus_bitrate);
                        break;
                    case FFmpegAudioCodec.libvorbis: // http://trac.ffmpeg.org/wiki/TheoraVorbisEncodingGuide
                        args.AppendFormat("-c:a libvorbis -qscale:a {0} ", FFmpeg.Vorbis_qscale);
                        break;
                    case FFmpegAudioCodec.libmp3lame: // http://trac.ffmpeg.org/wiki/Encode/MP3
                        args.AppendFormat("-c:a libmp3lame -qscale:a {0} ", FFmpeg.MP3_qscale);
                        break;
                }
            }

            if (!FFmpeg.IsFullSize)
            {
                args.AppendFormat("-vf \"scale=ceil(iw/4)*2:ceil(ih/4)*2\" ");
            }
            else
            {
                args.AppendFormat("-vf \"pad=ceil(iw/2)*2:ceil(ih/2)*2\" ");
            }

            if (Duration > 0)
            {
                args.AppendFormat("-t {0} ", isCustom ? "$duration$" : Duration.ToString("0.0", CultureInfo.InvariantCulture)); // duration limit
            }

            args.Append("-y "); // overwrite file

            string output;

            if (isCustom)
            {
                output = "$output$";
            }
            else
            {
                output = Path.ChangeExtension(OutputPath, IsLossless ? "mp4" : FFmpeg.Extension);
            }

            args.AppendFormat("\"{0}\"", output);

            return args.ToString();
        }
    }
}
