using PicView.Core.FileHistory;

namespace PicView.Tests.FileHistory;

/// <summary>
/// Keeps the user's own file history as it was around a test that touches <see cref="FileHistoryManager"/>.
/// </summary>
internal sealed class FileHistoryFileBackup() : ConfigFileBackup(new FileHistoryConfiguration());
