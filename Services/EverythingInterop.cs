using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace EverythingModern.Services
{
    public static class EverythingInterop
    {
        private const string EverythingDll = "Everything64.dll";

        public const uint EVERYTHING_REQUEST_FILE_NAME = 0x00000001;
        public const uint EVERYTHING_REQUEST_PATH = 0x00000002;
        public const uint EVERYTHING_REQUEST_FULL_PATH_AND_FILE_NAME = 0x00000004;
        public const uint EVERYTHING_REQUEST_EXTENSION = 0x00000008;
        public const uint EVERYTHING_REQUEST_SIZE = 0x00000010;
        public const uint EVERYTHING_REQUEST_DATE_CREATED = 0x00000020;
        public const uint EVERYTHING_REQUEST_DATE_MODIFIED = 0x00000040;
        public const uint EVERYTHING_REQUEST_DATE_ACCESSED = 0x00000080;
        public const uint EVERYTHING_REQUEST_ATTRIBUTES = 0x00000100;

        public const uint EVERYTHING_SORT_NAME_ASCENDING = 1;
        public const uint EVERYTHING_SORT_NAME_DESCENDING = 2;
        public const uint EVERYTHING_SORT_PATH_ASCENDING = 3;
        public const uint EVERYTHING_SORT_PATH_DESCENDING = 4;
        public const uint EVERYTHING_SORT_SIZE_ASCENDING = 5;
        public const uint EVERYTHING_SORT_SIZE_DESCENDING = 6;
        public const uint EVERYTHING_SORT_EXTENSION_ASCENDING = 7;
        public const uint EVERYTHING_SORT_EXTENSION_DESCENDING = 8;
        public const uint EVERYTHING_SORT_DATE_MODIFIED_ASCENDING = 13;
        public const uint EVERYTHING_SORT_DATE_MODIFIED_DESCENDING = 14;

        [DllImport(EverythingDll, CharSet = CharSet.Unicode)]
        public static extern void Everything_SetSearchW(string lpString);

        [DllImport(EverythingDll)]
        public static extern void Everything_SetMatchPath(bool bEnable);

        [DllImport(EverythingDll)]
        public static extern void Everything_SetMatchCase(bool bEnable);

        [DllImport(EverythingDll)]
        public static extern void Everything_SetMatchWholeWord(bool bEnable);

        [DllImport(EverythingDll)]
        public static extern void Everything_SetRegex(bool bEnable);

        [DllImport(EverythingDll)]
        public static extern void Everything_SetMax(uint dwMax);

        [DllImport(EverythingDll)]
        public static extern void Everything_SetOffset(uint dwOffset);

        [DllImport(EverythingDll)]
        public static extern void Everything_SetSort(uint dwSort);

        [DllImport(EverythingDll)]
        public static extern void Everything_SetRequestFlags(uint dwRequestFlags);

        [DllImport(EverythingDll)]
        public static extern bool Everything_QueryW(bool bWait);

        [DllImport(EverythingDll)]
        public static extern uint Everything_GetNumResults();

        [DllImport(EverythingDll)]
        public static extern uint Everything_GetTotResults();

        [DllImport(EverythingDll)]
        public static extern uint Everything_GetLastError();

        [DllImport(EverythingDll)]
        public static extern bool Everything_IsFolderResult(uint nIndex);

        [DllImport(EverythingDll, CharSet = CharSet.Unicode)]
        public static extern IntPtr Everything_GetResultFileNameW(uint nIndex);

        [DllImport(EverythingDll, CharSet = CharSet.Unicode)]
        public static extern IntPtr Everything_GetResultPathW(uint nIndex);

        [DllImport(EverythingDll, CharSet = CharSet.Unicode)]
        public static extern IntPtr Everything_GetResultExtensionW(uint nIndex);

        [DllImport(EverythingDll)]
        public static extern bool Everything_GetResultSize(uint nIndex, out long lpSize);

        [DllImport(EverythingDll)]
        public static extern bool Everything_GetResultDateModified(uint nIndex, out long lpFileTime);

        [DllImport(EverythingDll)]
        public static extern bool Everything_GetResultDateCreated(uint nIndex, out long lpFileTime);

        [DllImport(EverythingDll)]
        public static extern bool Everything_GetResultDateAccessed(uint nIndex, out long lpFileTime);

        [DllImport(EverythingDll)]
        public static extern uint Everything_GetResultAttributes(uint nIndex);

        [DllImport(EverythingDll)]
        public static extern void Everything_Reset();

        [DllImport(EverythingDll)]
        public static extern bool Everything_IsDBLoaded();

        [DllImport(EverythingDll)]
        public static extern bool Everything_IsAdmin();

        [DllImport(EverythingDll)]
        public static extern uint Everything_GetMajorVersion();

        [DllImport(EverythingDll)]
        public static extern uint Everything_GetMinorVersion();

        [DllImport(EverythingDll)]
        public static extern uint Everything_GetBuildNumber();
    }
}
