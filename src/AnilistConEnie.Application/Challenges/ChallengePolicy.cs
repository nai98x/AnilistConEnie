using AnilistConEnie.Model.Entities;

namespace AnilistConEnie.Application.Challenges;

public static class ChallengePolicy
{
    /// <summary>
    /// Un challenge disponible y con fecha de vencimiento se considera vencido cuando su vencimiento es hoy o
    /// ya pasó (es decir, falta menos de un día para el comienzo del día siguiente).
    /// </summary>
    public static bool EstaVencido(Challenge challenge, DateTime hoy) =>
        challenge.Vencimiento != null && challenge.Disponible && hoy.AddDays(1) > challenge.Vencimiento;
}
