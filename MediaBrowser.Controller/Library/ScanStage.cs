namespace MediaBrowser.Controller.Library;

/// <summary>
/// The stages of a library scan that <see cref="ScanTimings"/> measures.
/// </summary>
public enum ScanStage
{
    /// <summary>Listing a folder's files and resolving each path to an item.</summary>
    Enumerate,

    /// <summary>Reconciling items already in the library with what was found on disk.</summary>
    ExistingItems,

    /// <summary>Checking the image files of unchanged items; nested in <see cref="ExistingItems"/>.</summary>
    ExistingItemImages,

    /// <summary>Saving newly found items to the database.</summary>
    CreateItems,

    /// <summary>Working out which metadata providers need to run for an item.</summary>
    ChangeCheck,

    /// <summary>Registering local images (posters, backdrops) found next to an item.</summary>
    LocalImages,

    /// <summary>Running metadata providers, including ffprobe and remote lookups.</summary>
    Providers,

    /// <summary>The ffprobe call itself; nested in <see cref="Providers"/>.</summary>
    Ffprobe,

    /// <summary>Downloading remote images and measuring them.</summary>
    RemoteImages,

    /// <summary>Saving an item after its refresh.</summary>
    SaveItem
}
