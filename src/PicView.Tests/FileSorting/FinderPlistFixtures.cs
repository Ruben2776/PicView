#if MACOS
namespace PicView.Tests.FileSorting;

/// <summary>
/// Binary property lists as Finder saves them in a .DS_Store, made from XML plists with
/// <c>plutil -convert binary1</c>.
/// </summary>
internal static class FinderPlistFixtures
{
    /// <summary><c>{ arrangeBy = "dateLastOpened"; viewOptionsVersion = 1; iconSize = 64.0; showItemInfo = false; }</c></summary>
    internal static readonly byte[] Icon = Convert.FromBase64String(
        "YnBsaXN0MDDUAQIDBAUGBwhZYXJyYW5nZUJ5WGljb25TaXplXHNob3dJdGVtSW5mb18QEnZpZXdPcHRpb25zVmVyc2lvbl5kYXRlTGFzdE9wZW5lZCNAUAAAAAAAAAgQAQgRGyQxRlVeXwAAAAAAAAEBAAAAAAAAAAkAAAAAAAAAAAAAAAAAAABh");

    /// <summary>
    /// <c>{ sortColumn = "dateLastOpened"; columns = ( { identifier = "name"; ascending = true; visible = true;
    /// width = 300; }, { identifier = "dateLastOpened"; ascending = false; visible = true; width = 200; } ); }</c>
    /// </summary>
    internal static readonly byte[] ListNew = Convert.FromBase64String(
        "YnBsaXN0MDDSAQIDEFdjb2x1bW5zWnNvcnRDb2x1bW6iBA3UBQYHCAkKCwpVd2lkdGhZYXNjZW5kaW5nWmlkZW50aWZpZXJXdmlzaWJsZREBLAlUbmFtZQnUBQYHCA4PEAoQyAheZGF0ZUxhc3RPcGVuZWQJCA0VICMsMjxHT1JTWFliZGV0AAAAAAAAAQEAAAAAAAAAEgAAAAAAAAAAAAAAAAAAAHU=");

    /// <summary>
    /// <c>{ sortColumn = "name"; columns = { name = { ascending = true; index = 0; }; dateLastOpened =
    /// { ascending = false; index = 5; }; }; }</c>
    /// </summary>
    internal static readonly byte[] ListOld = Convert.FromBase64String(
        "YnBsaXN0MDDSAQIDBFdjb2x1bW5zWnNvcnRDb2x1bW7SBAUGC1RuYW1lXmRhdGVMYXN0T3BlbmVk0gcICQpVaW5kZXhZYXNjZW5kaW5nEAAJ0gcIDA0QBQgIDRUgJSo5PkROUFFWWAAAAAAAAAEBAAAAAAAAAA4AAAAAAAAAAAAAAAAAAABZ");

    /// <summary>
    /// <c>{ "größe" = "Ünïcode"; columns = ( { identifier = "Größe"; ascending = true; } ); }</c>: strings that are
    /// not ASCII, which a binary property list keeps as UTF-16.
    /// </summary>
    internal static readonly byte[] NotAscii = Convert.FromBase64String(
        "YnBsaXN0MDDSAQIDBGUAZwByAPYA3wBlV2NvbHVtbnNnANwAbgDvAGMAbwBkAGWhBdIGBwgJWWFzY2VuZGluZ1ppZGVudGlmaWVyCWUARwByAPYA3wBlCA0YIC8xNkBLTAAAAAAAAAEBAAAAAAAAAAoAAAAAAAAAAAAAAAAAAABX");
}
#endif
