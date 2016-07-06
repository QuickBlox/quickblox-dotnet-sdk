// This is the main DLL file.

#pragma once

#include <collection.h>

using namespace Platform;
using namespace Platform::Details;

#define VPX_CODEC_DISABLE_COMPAT 1

#include <vpx/vpx_encoder.h>
#include <vpx/vp8cx.h>
#define vp8cx (vpx_codec_vp8_cx())

#include <vpx/vpx_decoder.h>
#include <vpx/vp8dx.h>
#define vp8dx (vpx_codec_vp8_dx())

std::wstring stows(std::string s)
{
	std::wstring ws;
	ws.assign(s.begin(), s.end());
	return ws;
}

std::string wstos(std::wstring ws)
{
	std::string s;
	s.assign(ws.begin(), ws.end());
	return s;
}

Platform::String ^stops(std::string s)
{
	return ref new Platform::String(stows(s).c_str());
}

std::string pstos(Platform::String^ ps)
{
	return wstos(std::wstring(ps->Data()));
}

Platform::String ^atops(const char *text)
{
	return stops(std::string(text));
}

namespace Win8_VP8
{
	public ref class Nv12Bitmap sealed
	{
	private:
		int width;
		int height;

	public:
		property Array<uint8>^ Buffer;

		property int Width
		{
			int get() { return width; }
		}

		property int Height
		{
			int get() { return height; }
		}

		Nv12Bitmap(int width, int height)
			: width(width), height(height)
		{ }

		Nv12Bitmap(int width, int height, const Array<uint8>^ buffer)
			: width(width), height(height)
		{ 
			Buffer = buffer;
		}
	};

	public ref class Yuy2Bitmap sealed
	{
	private:
		int width;
		int height;

	public:
		property Array<uint8>^ Buffer;

		property int Width
		{
			int get() { return width; }
		}

		property int Height
		{
			int get() { return height; }
		}

		Yuy2Bitmap(int width, int height)
			: width(width), height(height)
		{ }

		Yuy2Bitmap(int width, int height, const Array<uint8>^ buffer)
			: width(width), height(height)
		{ 
			Buffer = buffer;
		}
	};

	public ref class Rgb32Bitmap sealed
	{
	private:
		int width;
		int height;

	public:
		property Array<uint8>^ Buffer;

		property int Width
		{
			int get() { return width; }
		}

		property int Height
		{
			int get() { return height; }
		}

		Rgb32Bitmap(int width, int height)
			: width(width), height(height)
		{ }
	};

	private ref class Converter sealed
	{
	private:
		static int getR(int y, int u, int v)
		{
			return clamp(y + (int)(1.402f * (v - 128)));
		}

		static int getG(int y, int u, int v)
		{
			return clamp(y - (int)(0.344f * (u - 128) + 0.714f * (v - 128)));
		}

		static int getB(int y, int u, int v)
		{
			return clamp(y + (int)(1.772f * (u - 128)));
		}

		static int getY(int r, int g, int b)
		{
			return (int)(0.299f * r + 0.587f * g + 0.114f * b);
		}

		static int getU(int r, int g, int b)
		{
			return (int)(-0.169f * r - 0.331f * g + 0.499f * b + 128);
		}

		static int getV(int r, int g, int b)
		{
			return (int)(0.499f * r - 0.418f * g - 0.0813f * b + 128);
		}

		static int clamp(int value)
		{
			if (value < 0)
			{
				return 0;
			}
			if (value > 255)
			{
				return 255;
			}
			return value;
		}

		static int isLittleEndian()
		{
			union {
				uint32_t i;
				char c[4];
			} bint = { 0x01020304 };

			return !(bint.c[0] == 1);
		}

	internal:
		static void ConvertYUY2ToI420(Yuy2Bitmap^ yuy2, vpx_image_t *yuvImage)
		{
			int yuy2Stride = yuy2->Width * 2;
			int yuvStride = yuy2->Width;

			int yuy2yi0 = 0;
			int yuy2yi1 = yuy2Stride;
			int yi0 = 0;
			int yi1 = yuvStride;
			int uvi = 0;
			for (int ypos = 0; ypos < yuy2->Height; ypos += 2)
			{
				for (int xpos = 0; xpos < yuy2->Width * 2; xpos += 4)
				{
					int y00 = yuy2->Buffer[yuy2yi0++];
					int u0 = yuy2->Buffer[yuy2yi0++];
					int y01 = yuy2->Buffer[yuy2yi0++];
					int v0 = yuy2->Buffer[yuy2yi0++];

					int y10 = yuy2->Buffer[yuy2yi1++];
					int u1 = yuy2->Buffer[yuy2yi1++];
					int y11 = yuy2->Buffer[yuy2yi1++];
					int v1 = yuy2->Buffer[yuy2yi1++];

					yuvImage->planes[VPX_PLANE_Y][yi0++] = (unsigned char)y00;
					yuvImage->planes[VPX_PLANE_Y][yi0++] = (unsigned char)y01;
					yuvImage->planes[VPX_PLANE_Y][yi1++] = (unsigned char)y10;
					yuvImage->planes[VPX_PLANE_Y][yi1++] = (unsigned char)y11;

					yuvImage->planes[VPX_PLANE_U][uvi] = (unsigned char)((u0 + u1) / 2);
					yuvImage->planes[VPX_PLANE_V][uvi] = (unsigned char)((v0 + v1) / 2);
					uvi++;
				}

				yuy2yi0 += yuy2Stride;
				yuy2yi1 += yuy2Stride;

				yi0 += yuvStride;
				yi1 += yuvStride;
			}
		}

		static void ConvertNV12ToI420(Nv12Bitmap^ nv12, vpx_image_t *nv2Image)
		{
			int uvStart = nv12->Width * nv12->Height;
			int uvEnd = nv12->Width * nv12->Height * 1.5;
			int uvi = 0;

			//memcpy(nv2Image->planes[VPX_PLANE_Y], nv12->Buffer, uvStart);
			//Platform::Runtime::InteropServices::
			for(int ypos = 0;ypos < uvStart; ypos++)
			{
				nv2Image->planes[VPX_PLANE_Y][ypos] = nv12->Buffer[ypos];
			}

			for(int uvpos = uvStart;uvpos < uvEnd; uvpos += 2)
			{
				nv2Image->planes[VPX_PLANE_U][uvi] = nv12->Buffer[uvpos];
				nv2Image->planes[VPX_PLANE_V][uvi] = nv12->Buffer[uvpos + 1];
				uvi++;
			}
		}

		static void ConvertI420ToRGB32(vpx_image_t *yuvImage, Rgb32Bitmap^ rgb32)
		{
			int yPadding = yuvImage->stride[VPX_PLANE_Y] - yuvImage->d_w;
			int uPadding = yuvImage->stride[VPX_PLANE_U] - (yuvImage->d_w / 2);
			int vPadding = yuvImage->stride[VPX_PLANE_V] - (yuvImage->d_w / 2);

			int yi = 0;
			int ui = 0;
			int vi = 0;
			int rgbi = 0;
			for (unsigned int ypos = 0; ypos < yuvImage->d_h; ypos++)
			{
				for (unsigned int xpos = 0; xpos < yuvImage->d_w; xpos++)
				{
					int y = yuvImage->planes[VPX_PLANE_Y][yi] & 0xFF;
					int u = yuvImage->planes[VPX_PLANE_U][ui] & 0xFF;
					int v = yuvImage->planes[VPX_PLANE_V][vi] & 0xFF;

					int r = getR(y, u, v);
					int g = getG(y, u, v);
					int b = getB(y, u, v);

					rgb32->Buffer[rgbi++] = (unsigned char)r;
					rgb32->Buffer[rgbi++] = (unsigned char)g;
					rgb32->Buffer[rgbi++] = (unsigned char)b;
					rgb32->Buffer[rgbi++] = (unsigned char)255; // alpha

					yi++;
					if (xpos % 2 == 1)
					{
						ui++;
						vi++;
					}
				}

				yi += yPadding;

				if (ypos % 2 == 0)
				{
					ui -= (yuvImage->d_w / 2);
					vi -= (yuvImage->d_w / 2);
				}
				else
				{
					ui += uPadding;
					vi += vPadding;
				}
			}
		}
	};

	public ref class Encoder sealed
	{
	private:
		vpx_codec_ctx_t      *codec;
		vpx_codec_enc_cfg_t  *config;
		vpx_image_t          *img;
		int                   frame_cnt;
        bool                  sendKeyFrame;

	public:
		property bool DebugMode;

		Encoder()
		{ }

		void Destroy()
		{
			if (codec)
			{
				vpx_codec_destroy(codec);
				delete codec;
				codec = nullptr;
			}

			if (config)
			{
				delete config;
				config = nullptr;
			}

			if (img)
			{
				vpx_img_free(img);
				img = nullptr;
			}
		}

		Array<uint8>^ Encode(Yuy2Bitmap^ yuy2)
		{
			return Encode(yuy2->Width, yuy2->Height, yuy2->Buffer, true);
		}

		Array<uint8>^ Encode(Nv12Bitmap^ nv12)
		{
			return Encode(nv12->Width, nv12->Height, nv12->Buffer, false);
		}

		Array<uint8>^ Encode(int width, int height, const Array<uint8>^ data, bool isYuy2)
		{
			// if the dimensions change, trash reusable data structures
			if (codec && (width != config->g_w || height != config->g_h))
			{
				if (codec)
				{
					vpx_codec_destroy(codec);
					delete codec;
					codec = nullptr;
				}

				if (config)
				{
					delete config;
					config = nullptr;
				}

				if (img)
				{
					vpx_img_free(img);
					img = nullptr;
				}
			}

			// create encoder
			if (!codec)
			{
				if (DebugMode)
				{
					Console::WriteLine(L"Configuring encoder to use new dimensions.");
				}

				// allocate config
				config = new vpx_codec_enc_cfg_t();

				// set config defaults
				vpx_codec_err_t res = vpx_codec_enc_config_default(vp8cx, config, 0);
				if (res)
				{
					delete config;
					config = nullptr;
						
					Console::WriteLine(L"Could not set encoder config defaults.");
					Console::WriteLine(atops(vpx_codec_err_to_string(res)));
					throw 0;
				}

				config->g_timebase.num = 1;
				config->g_timebase.den = 30;
				config->rc_target_bitrate = width * height * 256 / 320 / 240;
				config->rc_end_usage = VPX_CBR;
				config->g_w = width;
				config->g_h = height;
				config->kf_mode = VPX_KF_AUTO;
				config->kf_min_dist = config->kf_max_dist = 30;
				config->g_error_resilient = 1;
				config->g_lag_in_frames = 0;
				config->g_pass = VPX_RC_ONE_PASS;
				config->rc_min_quantizer = 0;
				config->rc_max_quantizer = 63;
				config->g_profile = 0;

				// allocate encoder
				codec = new vpx_codec_ctx_t();

				// initialize encoder
				res = vpx_codec_enc_init(codec, vp8cx, config, 0);
				if (res)
				{
					delete codec;
					codec = nullptr;
						
					Console::WriteLine(L"Could not initialize encoder.");
					Console::WriteLine(atops(vpx_codec_err_to_string(res)));
					throw 0;
				}

				// additional tuning
				vpx_codec_control(codec, VP8E_SET_STATIC_THRESHOLD, 1);
				vpx_codec_control(codec, VP8E_SET_TOKEN_PARTITIONS, VP8_ONE_TOKENPARTITION);
				vpx_codec_control(codec, VP8E_SET_NOISE_SENSITIVITY, 0);
					
				img = vpx_img_alloc(NULL, VPX_IMG_FMT_I420, width, height, 0);
			}
                
			// convert YUY2 to I420
			if(isYuy2)
			{
				Yuy2Bitmap^ yuy2bitmap = ref new Yuy2Bitmap(width, height, data);
				Converter::ConvertYUY2ToI420(yuy2bitmap, img);
			}
			else
			{
				Nv12Bitmap^ nv12bitmap = ref new Nv12Bitmap(width, height, data);
				Converter::ConvertNV12ToI420(nv12bitmap, img);
			}
                
            // set flag
            vpx_enc_frame_flags_t flag = 0;
            if (sendKeyFrame)
            {
                if (DebugMode)
                {
					Console::WriteLine(L"Forcing keyframe.");
                }
                    
                flag |= VPX_EFLAG_FORCE_KF;
                sendKeyFrame = false;
            }

			// encode
			vpx_codec_err_t res = vpx_codec_encode(codec, img, frame_cnt, 1, flag, VPX_DL_REALTIME);
			if (res)
			{
				if (DebugMode)
				{
					Console::WriteLine(L"Could not encode frame.");
					Console::WriteLine(atops(vpx_codec_err_to_string(res)));
				}
				return nullptr;
			}

			frame_cnt++;

			// get frame
			vpx_codec_iter_t iter = NULL;
			const vpx_codec_cx_pkt_t *pkt;
			while (pkt = vpx_codec_get_cx_data(codec, &iter))
			{
				if (pkt->kind == VPX_CODEC_CX_FRAME_PKT)
				{
					// copy unmanaged buffer to managed buffer
					return ref new Array<uint8>((unsigned char *)pkt->data.frame.buf, pkt->data.frame.sz);
				}
			}

			if (DebugMode)
			{
				Console::WriteLine(atops("Could not encode frame."));
			}
			return nullptr;
		}

		void ForceKeyframe()
		{
            sendKeyFrame = true;
		}
	};

	public ref class Decoder sealed
	{
	private:
		vpx_codec_ctx_t *codec;

	public:
		property bool DebugMode;
		property bool NeedsKeyFrame;

		Decoder()
		{
			// initialize decoder
			codec = new vpx_codec_ctx_t();
			vpx_codec_err_t res = vpx_codec_dec_init(codec, vp8dx, NULL, 0);
			if (res)
			{
				Console::WriteLine(L"Could not initialize decoder.");
				Console::WriteLine(atops(vpx_codec_err_to_string(res)));
				throw 0;
			}
		}

		void Destroy()
		{
			vpx_codec_destroy(codec);
			delete codec;
		}

		Rgb32Bitmap^ Decode(const Array<uint8>^ encodedFrame)
		{
			unsigned char *encFrame = encodedFrame->Data;

			// decode
			vpx_codec_err_t res = vpx_codec_decode(codec, encFrame, encodedFrame->Length, NULL, 0);
			if (res)
			{
				if (res == 5) // need a keyframe
				{
					NeedsKeyFrame = true;
				}
				if (DebugMode)
				{
					Console::WriteLine(L"Could not decode frame.");
					Console::WriteLine(atops(vpx_codec_err_to_string(res)));
				}
				return nullptr;
			}

			NeedsKeyFrame = false;

			// get frame
			vpx_image_t *img;
			vpx_codec_iter_t iter = NULL;
			while (img = vpx_codec_get_frame(codec, &iter))
			{
				Rgb32Bitmap^ rgb32 = ref new Rgb32Bitmap(img->d_w, img->d_h);
				rgb32->Buffer = ref new Array<uint8>(img->d_w * img->d_h * 4);

				// convert I420 to RGB
				Converter::ConvertI420ToRGB32(img, rgb32);

				return rgb32;
			}

			if (DebugMode)
			{
				Console::WriteLine(L"Could not decode frame.");
			}
			return nullptr;
		}
	};
}
