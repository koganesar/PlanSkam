using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planscam.DataAccess;
using Planscam.Entities;
using Planscam.Extensions;
using Planscam.FsServices;
using Planscam.Models;

namespace Planscam.Controllers;

public class PlaylistsController : PsmControllerBase
{
    private readonly PlaylistsRepo _playlistsRepo;

    public PlaylistsController(AppDbContext dataContext, UserManager<User> userManager,
        SignInManager<User> signInManager, PlaylistsRepo playlistsRepo) :
        base(dataContext, userManager, signInManager) =>
        _playlistsRepo = playlistsRepo;

    [HttpGet, Route(nameof(FavoriteTracks)), Authorize]
    public async Task<IActionResult> FavoriteTracks() =>
        RedirectToAction("Index", new
        {
            Id = await _playlistsRepo.GetFavouriteTracksId(User)
        });

    [HttpGet]
    public async Task<IActionResult> Index(int id) =>
        await _playlistsRepo.GetPlaylistFull(id, User) switch
        {
            { } playlist => View(playlist),
            _ => NotFound()
        };

    [HttpGet]
    public async Task<IActionResult> All() =>
        View(await DataContext.Playlists
            .Where(playlist => DataContext.FavouriteTracks.All(tracks => tracks != playlist))
            .Include(playlist => playlist.Picture)
            .AsNoTracking()
            .Select(playlist => new Playlist
            {
                Id = playlist.Id,
                Name = playlist.Name,
                Picture = playlist.Picture,
                IsAlbum = playlist.IsAlbum,
                IsLiked = SignInManager.IsSignedIn(User)
                    ? playlist.Users!.Any(user => user.Id == CurrentUserId)
                    : null,
                IsOwned = SignInManager.IsSignedIn(User)
                    ? CurrentUserQueryable
                        .Select(user => user.OwnedPlaylists!.Playlists!)
                        .Any(playlists => playlists.Any(playlist1 => playlist1 == playlist))
                    : null
            })
            .ToListAsync());

    [NonAction, Obsolete("переписано под ajax")] //[HttpPost, Authorize]
    public async Task<IActionResult> LikePlaylist(int id, string? returnUrl)
    {
        var playlist = await DataContext.Playlists
            .AsNoTracking()
            .FirstOrDefaultAsync(playlist => playlist.Id == id);
        if (playlist is null) return BadRequest();
        (await CurrentUserQueryable
            .Include(user => user.Playlists!.Where(_ => false))
            .FirstAsync()).Playlists!.Add(playlist);
        await DataContext.SaveChangesAsync();
        return IsLocalUrl(returnUrl)
            ? Redirect(returnUrl!)
            : RedirectToAction("Index", new {id});
    }

    [HttpPost, Authorize]
    public IActionResult LikePlaylist(int id) =>
        _playlistsRepo.LikePlaylist(User, id)
            ? Ok()
            : BadRequest();

    [NonAction, Obsolete("переписано под ajax")] //[HttpPost, Authorize]
    public async Task<IActionResult> UnlikePlaylist(int id, string? returnUrl)
    {
        CurrentUser = await CurrentUserQueryable
            .Include(user => user.Playlists!.Where(playlist => playlist.Id == id))
            .FirstAsync();
        if (!CurrentUser.Playlists!.Any()) return BadRequest();
        CurrentUser.Playlists!.Clear();
        await DataContext.SaveChangesAsync();
        return IsLocalUrl(returnUrl)
            ? Redirect(returnUrl!)
            : RedirectToAction("Index", new {id});
    }

    [HttpPost, Authorize]
    public IActionResult UnlikePlaylist(int id) =>
        _playlistsRepo.UnlikePlaylist(User, id)
            ? Ok()
            : BadRequest();

    [HttpGet, Authorize]
    public async Task<IActionResult> Liked()
    {
        CurrentUser = await CurrentUserQueryable
            .Include(user => user.Playlists!)
            .ThenInclude(playlist => playlist.Picture)
            .AsNoTracking()
            .FirstAsync();
        CurrentUser.Playlists!.ForEach(playlist => playlist.IsLiked = true);
        return View(CurrentUser);
    }

    [HttpGet, Authorize]
    public IActionResult Create() =>
        View();

    [HttpPost, Authorize]
    public IActionResult Create(CreatePlaylistViewModel model) =>
        ModelState.IsValid
            ? RedirectToAction("Index",
                new {_playlistsRepo.CreatePlaylist(User, model.Name, model.Picture.ToPicture()).Id})
            : View();

    [HttpGet, Authorize]
    public async Task<IActionResult> Delete(int id, string? returnUrl) =>
        await DataContext.Playlists
            .AnyAsync(playlist =>
                playlist.Id == id && CurrentUserQueryable.Select(user => user.OwnedPlaylists!.Playlists!)
                    .Any(playlists => playlists.Contains(playlist)))
            ? View(new DeletePlaylistViewModel
            {
                Id = id,
                ReturnUrl = returnUrl
            })
            : BadRequest();

    [HttpPost, Authorize]
    public IActionResult DeleteSure(int id, string? returnUrl) =>
        _playlistsRepo.DeletePlaylist(User, id)
            ? IsLocalUrl(returnUrl)
                ? Redirect(returnUrl!)
                : RedirectToAction("Liked")
            : BadRequest();

    [NonAction, Obsolete("переписано под ajax")] //[HttpPost, Authorize]
    public async Task<IActionResult> AddTrackToPlaylist(int playlistId, int trackId, string returnUrl)
    {
        var playlist = await DataContext.Playlists
            .Include(playlist => playlist.Tracks)
            .FirstOrDefaultAsync(playlist => playlist.Id == playlistId);
        if (playlist is null
            || playlist.Tracks!.Any(t => t.Id == trackId) //не добавлен ли уже этот трек в плейлист
            || !CurrentUserQueryable
                .Select(user => user.OwnedPlaylists!.Playlists!)
                .Any(playlists => playlists.Contains(playlist)) //принадлежит ли плейлист юзеру
            || DataContext.Tracks.FirstOrDefault(t => t.Id == trackId) is not { } track) //существует ли трек
            return BadRequest();
        playlist.Tracks!.Add(track);
        await DataContext.SaveChangesAsync();
        return IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction("Index", "Tracks", new {Id = trackId});
    }

    [HttpPost, Authorize]
    public IActionResult AddTrackToPlaylist(int playlistId, int trackId) =>
        _playlistsRepo.AddTrackToPlaylist(User, playlistId, trackId)
            ? BadRequest()
            : Ok();

    [HttpGet]
    public IActionResult GetData(int id) =>
        _playlistsRepo.GetData(id) switch
        {
            { } playlist => Json(playlist),
            _ => NotFound()
        };
}
