// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TimeManager.Data;
using TimeManager.Models;

namespace TimeManager.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly TimeManagerDbContext _context;

        public async Task<string> GetTelegramIdAsync(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user), "Пользователь не может быть null.");
            }

            // Асинхронно получаем email пользователя
            var email = await _userManager.GetEmailAsync(user);

            if (string.IsNullOrEmpty(email))
            {
                throw new InvalidOperationException("У пользователя отсутствует email.");
            }

            // Находим пользователя в базе данных по email
            var userFromDb = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            // Возвращаем TelegramId, если пользователь найден
            return userFromDb?.TelegramId;
        }
        
        
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
       
        public IndexModel(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            TimeManagerDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [StringLength(50, ErrorMessage = "The {0} must be at max {1} characters long.")]
            [Display(Name = "Telegram ID")]
            public string TelegramId { get; set; }
            
            
            [Phone]
            [Display(Name = "Phone number")]
            public string PhoneNumber { get; set; }
        }

        private async Task LoadAsync(User user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            var telegramId = await GetTelegramIdAsync(user);
            Username = userName;

            Input = new InputModel
            {
                PhoneNumber = phoneNumber,
                TelegramId = telegramId
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Невозможно загрузить пользователя с идентификатором: '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Невозможно загрузить пользователя с идентификатором: '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Неизвестная ошибка при попытке изменить номер телефона";
                    return RedirectToPage();
                }
            }
            
            var telegramId = await GetTelegramIdAsync(user);
            if (Input.TelegramId != telegramId)
            {
                var email = await _userManager.GetEmailAsync(user);
                var _user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (_user == null)
                {
                    throw new InvalidOperationException("Пользователь с таким email не найден.");
                }

                // Обновляем TelegramId
                _user.TelegramId = Input.TelegramId;

                // Сохраняем изменения в базе данных
                await _context.SaveChangesAsync();
            }
            
            
            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Ваш профиль был обновлен";
            return RedirectToPage();
        }
    }
}
