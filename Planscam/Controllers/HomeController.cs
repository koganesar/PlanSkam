﻿using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planscam.DataAccess;
using Planscam.Entities;
using Planscam.Models;

namespace Planscam.Controllers;

public class HomeController : PsmControllerBase
{
    public HomeController(AppDbContext dataContext, UserManager<User> userManager, SignInManager<User> signInManager) :
        base(dataContext, userManager, signInManager)
    {
    }

    //todo переписать полностью
    public async Task<IActionResult> Index()
    {
        var playlists = await DataContext.Playlists
            .Include(playlist => playlist.Picture)
            .AsNoTracking()
            .ToListAsync();
        if (!SignInManager.IsSignedIn(User)) return View(new HomePageViewModel {Playlists = playlists});
        CurrentUser = await CurrentUserQueryable
            .Include(user => user.Picture)
            .Include(user => user.FavouriteTracks)
            .Include(user => user.FavouriteTracks!.Picture)
            .AsNoTracking()
            .FirstAsync();
        CurrentUser.FavouriteTracks!.Tracks = DataContext.Tracks
            .Where(track => track.Playlists!.Contains(CurrentUser.FavouriteTracks!))
            .Include(track => track.Picture)
            .AsNoTracking()
            .ToList();
        return View(new HomePageViewModel
        {
            Playlists = playlists,
            User = CurrentUser
        });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
}
