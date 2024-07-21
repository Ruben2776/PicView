﻿using System.Diagnostics;

namespace PicView.Core.Config;

public static class VersionHelper
{
    public static FileVersionInfo GetFileVersionInfo()
    {
        var loc = ProcessHandling.ProcessHelper.GetPathToProcess();
        var fvi = FileVersionInfo.GetVersionInfo(loc);
        return fvi;
    }
}