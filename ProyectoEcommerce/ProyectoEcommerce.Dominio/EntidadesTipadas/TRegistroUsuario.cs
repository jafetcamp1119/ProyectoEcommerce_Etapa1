using System.ComponentModel.DataAnnotations;

namespace ProyectoEcommerce.Dominio.EntidadesTipadas;

public class TRegistroUsuario
{
    private string _nombre = string.Empty;
    private string _apellidos = string.Empty;
    private string _correo = string.Empty;
    private string _telefono = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(80, ErrorMessage = "El nombre no puede superar 80 caracteres.")]
    public string Nombre
    {
        get => _nombre;
        set => _nombre = value?.Trim() ?? string.Empty;
    }

    [Required(ErrorMessage = "Los apellidos son obligatorios.")]
    [MaxLength(120, ErrorMessage = "Los apellidos no pueden superar 120 caracteres.")]
    public string Apellidos
    {
        get => _apellidos;
        set => _apellidos = value?.Trim() ?? string.Empty;
    }

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo electrónico válido.")]
    [MaxLength(120, ErrorMessage = "El correo no puede superar 120 caracteres.")]
    public string Correo
    {
        get => _correo;
        set => _correo = value?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [MaxLength(30, ErrorMessage = "El teléfono no puede superar 30 caracteres.")]
    public string Telefono
    {
        get => _telefono;
        set => _telefono = value?.Trim() ?? string.Empty;
    }

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    [MaxLength(100, ErrorMessage = "La contraseña no puede superar 100 caracteres.")]
    public string Contrasena { get; set; } = null!;

    [Required(ErrorMessage = "La confirmación de contraseña es obligatoria.")]
    [Compare(nameof(Contrasena), ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmarContrasena { get; set; } = null!;
}
