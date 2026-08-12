namespace ProyectoEcommerce.Dominio.EntidadesTipadas;

/// <summary>Indica si LessPrice todavía necesita crear su primer Administrador.</summary>
public class TEstadoConfiguracionInicial
{
    public bool RequiereConfiguracionInicial { get; set; }
    public string CorreoAdministradorInicial { get; set; } = string.Empty;
}
