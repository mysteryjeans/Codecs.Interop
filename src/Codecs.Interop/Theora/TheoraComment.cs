using System;
using System.Runtime.InteropServices;

namespace Codecs.Interop.Theora
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct th_comment
    {
        /**The array of comment string vectors.*/
        IntPtr user_comments;
        /**An array of the corresponding length of each vector, in bytes.*/
        IntPtr comment_lengths; //int*
        /**The total number of comment strings.*/
        int comments;
        /**The null-terminated vendor string.
           This identifies the software used to encode the stream.*/
        IntPtr vendor;  //char*
    }

    public class TheoraComment
    {
        #region Theora Comment

        [DllImport(Theora.DllFile, CallingConvention = CallingConvention.Cdecl)]
        private extern static void th_comment_init([In, Out] ref th_comment _tc);

        public static void Init(TheoraComment comment)
        {
            th_comment_init(ref comment.th_comment);
            comment.changed = true;
        }

        [DllImport(Theora.DllFile, CallingConvention = CallingConvention.Cdecl)]
        private extern static void th_comment_clear([In] ref th_comment _tc);

        public static void Clear(TheoraComment comment)
        {
            th_comment_clear(ref comment.th_comment);
            comment.changed = true;
        }

        #endregion

        internal bool changed = true;
        internal th_comment th_comment = new th_comment();
    }
}
