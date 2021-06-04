/********************************************************************
 *                                                                  *
 * THIS FILE IS PART OF THE OggVorbis SOFTWARE CODEC SOURCE CODE.   *
 * USE, DISTRIBUTION AND REPRODUCTION OF THIS LIBRARY SOURCE IS     *
 * GOVERNED BY A BSD-STYLE SOURCE LICENSE INCLUDED WITH THIS SOURCE *
 * IN 'COPYING'. PLEASE READ THESE TERMS BEFORE DISTRIBUTING.       *
 *                                                                  *
 * THE OggVorbis SOURCE CODE IS (C) COPYRIGHT 1994-2002             *
 * by the Xiph.Org Foundation http://www.xiph.org/                  *
 *                                                                  *
 ********************************************************************

 function: toplevel libogg include

 ********************************************************************/

/* C#/.NET interop-port
 * 
 * Copyright 2004 Klaus Prückl <klaus.prueckl@aon.at>
 */

using System;
using System.Configuration;
using System.Runtime.InteropServices;

namespace Codecs.Interop.Theora
{
    internal abstract class Theora
    {
        internal const int TH_ENCCTL_SET_KEYFRAME_FREQUENCY_FORCE = 4;

        internal const int TH_ENCCTL_GET_SPLEVEL_MAX = 12;

        internal const int TH_ENCCTL_SET_SPLEVEL = 14;

        internal const int TH_ENCCTL_GET_SPLEVEL = 16;

        internal const int TH_ENCCTL_SET_DUP_COUNT = 18;

        internal const int TH_ENCCTL_SET_BITRATE = 30;

        internal const string DllFile = @"x64/libtheora.dll";
    }
}
