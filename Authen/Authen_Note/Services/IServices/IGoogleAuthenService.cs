﻿using Common.DTOs.GoogleAuthenticationDTOs;
using Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.IServices
{
    public interface IGoogleAuthenService
    {
        Task<ServiceResult> LoginWithGoogleAsync(string idToken);

    }
}
