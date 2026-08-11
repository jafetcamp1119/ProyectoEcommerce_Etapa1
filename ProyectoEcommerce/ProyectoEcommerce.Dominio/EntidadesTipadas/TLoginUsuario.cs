using System.ComponentModel.DataAnnotations;

namespace ProyectoEcommerce.Dominio.EntidadesTipadas;

/// <summary>Credenciales recibidas únicamente para validar el inicio de sesión.</summary>
public class TLoginUsuario
{
    private string _correo = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo electrónico válido.")]
    [MaxLength(120, ErrorMessage = "El correo no puede superar 120 caracteres.")]
    public string Correo
    {
        get => _correo;
        set => _correo = value?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Contrasena { get; set; } = null!;
}
