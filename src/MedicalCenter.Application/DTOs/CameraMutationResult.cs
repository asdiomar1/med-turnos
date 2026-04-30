namespace MedicalCenter.Application.DTOs;

public sealed record CameraMutationResult(
    CameraSummary Camara,
    int Movidos = 0,
    int Cancelados = 0,
    int ApartadosLiberados = 0,
    int Eliminados = 0);
