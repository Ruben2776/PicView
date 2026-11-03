using ObservableCollections;
using PicView.Avalonia.UI;

namespace PicView.Tests.UI;

public class DeferredListMirrorTests
{
    private readonly ObservableList<string> _source = ["a", "b"];
    private readonly Queue<Action> _posts = new();
    private readonly DeferredListMirror<string> _mirror;

    public DeferredListMirrorTests()
    {
        _mirror = new DeferredListMirror<string>(_source, _posts.Enqueue);
    }

    [Fact]
    public void New_CopiesTheSourceAtOnce()
    {
        Assert.Equal(["a", "b"], _mirror.Items);
        Assert.Empty(_posts);
    }

    [Fact]
    public void Change_BeforeItsPostRuns_LeavesTheMirrorAsItWas()
    {
        _source.Add("c");
        _source.Move(2, 0);
        _source.RemoveAt(1);

        Assert.Equal(["a", "b"], _mirror.Items);
    }

    [Fact]
    public void Fill_PostsOneChangePerItem()
    {
        _source.Clear();
        RunPosts();

        for (var i = 0; i < 5; i++)
        {
            _source.Add($"{i}");
        }

        // One post per row, so a view builds the rows between frames instead of all in one
        Assert.Equal(5, _posts.Count);
    }

    [Fact]
    public void Changes_AfterTheirPostsRun_MatchTheSource()
    {
        _source.Insert(0, "c");
        _source.Add("d");
        _source.Move(3, 1);
        _source.RemoveAt(2);
        _source[0] = "e";
        _source.Clear();
        _source.Add("f");
        _source.Insert(0, "g");
        _source.AddRange(["h", "i"]);

        RunPosts();

        Assert.Equal(_source, _mirror.Items);
    }

    [Fact]
    public void Dispose_StopsFollowingTheSource()
    {
        _mirror.Dispose();

        _source.Add("c");

        Assert.Empty(_posts);
    }

    private void RunPosts()
    {
        while (_posts.TryDequeue(out var post))
        {
            post();
        }
    }
}
