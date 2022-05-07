﻿using System.Threading.Tasks;
using HonzaBotner.Services.Contract.Dto;
namespace HonzaBotner.Services.Contract;

public interface IStandUpStatsService
{
    Task<StandUpStat?> GetStreak(ulong userId);

    Task UpdateStreak(ulong userId);

    Task<bool> IsValidStreak(ulong userId);

    Task UpdateStats(ulong userId, int completed, int total);
}
