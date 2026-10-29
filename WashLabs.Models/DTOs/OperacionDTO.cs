using System.ComponentModel.DataAnnotations;

namespace WashLabs.Models.DTOs;

public sealed record ClienteOpcionDTO(int ClienteId, string Nombre);
public sealed record VehiculoDTO(int VehiculoId, int ClienteId, string Cliente, string Matricula,
    string Marca, string Modelo, short? Anio, string? Color, string CategoriaTamano);
public sealed class RegistroVehiculoDTO
{
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un cliente.")]
    public int ClienteId { get; set; }
    [Required(ErrorMessage = "Indica la matrícula."), StringLength(15)]
    [RegularExpression(@"^[A-Za-z0-9-]{2,15}$", ErrorMessage = "Usa de 2 a 15 letras, números o guiones.")]
    public string Matricula { get; set; } = "";
    [Required(ErrorMessage = "Indica la marca."), StringLength(60)]
    public string Marca { get; set; } = "";
    [Required(ErrorMessage = "Indica el modelo."), StringLength(80)]
    public string Modelo { get; set; } = "";
    [Range(1950, 2100, ErrorMessage = "El año debe estar entre 1950 y 2100.")]
    public short? Anio { get; set; }
    [StringLength(40)] public string? Color { get; set; }
    [Required, RegularExpression("^(PEQUENO|MEDIANO|GRANDE|SUV|PICKUP|OTRO)$", ErrorMessage = "Selecciona un tamaño válido.")]
    public string CategoriaTamano { get; set; } = "MEDIANO";
}
public sealed record RegistroVehiculoResultadoDTO(bool Exito, string? Error);
public sealed record MovimientoLavadoDTO(long CitaId, DateTime Fecha, string Descripcion, int Cantidad);
public sealed record WashPointsVehiculoDTO(VehiculoDTO Vehiculo, int Lavados, int Meta, string Recompensa,
    bool RecompensaDisponible, IReadOnlyList<MovimientoLavadoDTO> Movimientos);
public sealed record CategoriaAsistenteDTO(int CategoriaId, string Nombre, string? Descripcion, int? PreguntaInicialId);
public sealed record OpcionAsistenteDTO(int OpcionId, string Texto, string? Respuesta, int? SiguientePreguntaId);
public sealed record PreguntaAsistenteDTO(int PreguntaId, string Categoria, string Pregunta,
    IReadOnlyList<OpcionAsistenteDTO> Opciones);
