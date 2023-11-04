using PicView.Tools;

if (args.Length is 0)
{
    Environment.Exit(-1);
}

if (string.IsNullOrEmpty(args[0]))
{
    Environment.Exit(-1);
}

var arg = args[0];

if (arg.StartsWith("lockscreen"))
{
    var path = arg[(arg.LastIndexOf(',') + 1)..];
    path = Path.GetFullPath(path);
    LockScreenHelper.SetLockScreenImage(path);

#if DEBUG
    Console.WriteLine(path);
    Console.ReadKey();
#endif
}