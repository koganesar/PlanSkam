using Planscam.Entities;

namespace Planscam.Models;

public class HomePageViewModel
{
    public User? User { get; set; } = null!;
    public List<Playlist> Playlists { get; set; } = null!;
}
