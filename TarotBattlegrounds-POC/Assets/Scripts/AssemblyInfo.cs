using System.Runtime.CompilerServices;

// EditMode tests drive internal seams (e.g. IgniteButton.EvaluateLongPress)
// without widening the public API.
[assembly: InternalsVisibleTo("Tests")]
