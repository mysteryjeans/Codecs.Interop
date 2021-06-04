/********************************************************************
 *                                                                  *
 * THIS FILE IS PART OF THE OggVorbis SOFTWARE CODEC SOURCE CODE.   *
 * USE, DISTRIBUTION AND REPRODUCTION OF THIS LIBRARY SOURCE IS     *
 * GOVERNED BY A BSD-STYLE SOURCE LICENSE INCLUDED WITH THIS SOURCE *
 * IN 'COPYING'. PLEASE READ THESE TERMS BEFORE DISTRIBUTING.       *
 *                                                                  *
 * THE OggVorbis SOURCE CODE IS (C) COPYRIGHT 1994-2001             *
 * by the XIPHOPHORUS Company http://www.xiph.org/                  *

 ********************************************************************

 function: libvorbis codec headers

 ********************************************************************/

/* C#/.NET interop-port
 * 
 * Copyright 2004 Klaus Prückl <klaus.prueckl@aon.at>
 */

using System;
using System.Runtime.InteropServices;
using Codecs.Interop.Ogg;

namespace Codecs.Interop.Vorbis
{
	/// <summary>
	/// vorbis_dsp_state buffers the current vorbis audio
	/// analysis/synthesis state.  The DSP state belongs to a specific
	/// logical bitstream
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	internal struct vorbis_dsp_state
	{
		internal int		analysisp;
		internal IntPtr		vi;

		internal IntPtr		pcm;	// float **
		internal IntPtr		pcmret;	// float **
		internal int		pcm_storage;
		internal int		pcm_current;
		internal int		pcm_returned;

		internal int		preextrapolate;
		internal int		eofflag;

		internal int		lW;
		internal int		W;
		internal int		nW;
		internal int		centerW;

		internal long	granulepos;
		internal long	sequence;

		internal long	glue_bits;
		internal long	time_bits;
		internal long	floor_bits;
		internal long	res_bits;

		internal IntPtr	backend_state;	// void *
	}

	/// <summary>
	/// </summary>
	public class VorbisDspState : IDisposable
	{
		#region Variables
		internal vorbis_dsp_state vorbis_dsp_state = new vorbis_dsp_state();
		private VorbisInfo vi;
        internal bool changed = true;
        private bool initialized = false;
		#endregion

		#region Constructor(s) & Destructor
		/// <summary>
		/// </summary>
		/// <param name="vi"></param>
		public VorbisDspState(VorbisInfo vi) 
		{
			this.vi = vi;
		}
		#endregion

		#region Properties
		/// <summary>
		/// </summary>
		public VorbisInfo Vi 
		{
			get 
			{
				if(this.changed) 
				{
					this.vi.vorbis_info = (vorbis_info) Marshal.PtrToStructure(
						this.vorbis_dsp_state.vi, typeof(vorbis_info));
					this.changed = false;
				}
				return this.vi;
			}
		}
		/// <summary>
		/// </summary>
		public int PcmStorage 
		{
			get { return this.vorbis_dsp_state.pcm_storage; }
		}
		/// <summary>
		/// </summary>
		public int PcmCurrent 
		{
			get { return this.vorbis_dsp_state.pcm_current; }
		}
		/// <summary>
		/// </summary>
		public long Sequence 
		{
			get { return this.vorbis_dsp_state.sequence; }
		}
		#endregion

		#region Vorbis PRIMITIVES: general ***************************************

        #region Analysis

        [DllImport(Vorbis.DllFile, CallingConvention = CallingConvention.Cdecl)]
        private static extern int vorbis_analysis_init([In, Out] ref vorbis_dsp_state v, [In, Out] ref vorbis_info vi);

        [DllImport(Vorbis.DllFile, CallingConvention = CallingConvention.Cdecl)]
        private static extern int vorbis_analysis_init(IntPtr v, [In, Out] ref vorbis_info vi);

        public static int AnalysisInit(VorbisDspState dspState, VorbisInfo info)
        {
            int retVal = vorbis_analysis_init(ref dspState.vorbis_dsp_state, ref info.vorbis_info);
            dspState.changed = true;
            dspState.initialized = true;
            info.changed = true;

            return retVal;
        }

        public static int AnalysisInit(IntPtr dspState, VorbisInfo info)
        {
            int retVal = vorbis_analysis_init(dspState, ref info.vorbis_info);
            info.changed = true;

            return retVal;
        }

        [DllImport(Vorbis.DllFile, CallingConvention = CallingConvention.Cdecl)]
        private static extern int vorbis_analysis_headerout([In, Out] ref vorbis_dsp_state v,
                                          [In, Out] ref vorbis_comment vc,
                                          [Out] out ogg_packet op,
                                          [Out] out ogg_packet op_comm,
                                          [Out] out ogg_packet op_code);

        [DllImport(Vorbis.DllFile, CallingConvention = CallingConvention.Cdecl)]
        private static extern int vorbis_analysis_headerout(IntPtr v,
                                          [In, Out] ref vorbis_comment vc,
                                          [Out] out ogg_packet op,
                                          [Out] out ogg_packet op_comm,
                                          [Out] out ogg_packet op_code);

        public static int HeaderOut(VorbisDspState dspState, VorbisComment comment, OggPacket header, OggPacket headerComm, OggPacket headerCode)
        {
            int retVal = vorbis_analysis_headerout(
                ref dspState.vorbis_dsp_state,
                ref comment.vorbis_comment,
                out header.ogg_packet,
                out headerComm.ogg_packet,
                out headerCode.ogg_packet);

            dspState.changed = true;
            comment.changed = true;
            header.changed = true;
            headerCode.changed = true;
            headerComm.changed = true;

            return retVal;
        }

        public static int HeaderOut(IntPtr dspState, VorbisComment comment, OggPacket header, OggPacket headerComm, OggPacket headerCode)
        {
            int retVal = vorbis_analysis_headerout(
                dspState,
                ref comment.vorbis_comment,
                out header.ogg_packet,
                out headerComm.ogg_packet,
                out headerCode.ogg_packet);

            comment.changed = true;
            header.changed = true;
            headerCode.changed = true;
            headerComm.changed = true;

            return retVal;
        }
        #endregion

        #region vorbis_dsp_clear
        [DllImport(Vorbis.DllFile, CallingConvention=CallingConvention.Cdecl)]
		private static extern void	vorbis_dsp_clear([In,Out] ref vorbis_dsp_state v);
		/// <summary>
		/// </summary>
		/// <param name="v"></param>
		public static void Clear(VorbisDspState v) 
		{
			vorbis_dsp_clear(ref v.vorbis_dsp_state);
			v.changed = true;
            v.initialized = false;
		}
		#endregion

		#endregion

        ~VorbisDspState()
        {
            this.Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.initialized)
                VorbisDspState.Clear(this);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
