using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PicView.Avalonia.Services;

public interface IPlatformSpecificService
{
    void SetCursorPos(int x, int y);

    List<string> GetFiles(FileInfo fileInfo);

    int CompareStrings(string str1, string str2);
}