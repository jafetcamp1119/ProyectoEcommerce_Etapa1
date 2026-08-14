namespace ProyectoEcommerce.Dominio.EntidadesTipadas;

public class TEstadoConfiguracionInicial
{
    public bool RequiereConfiguracionInicial { get; set; }
    public string CorreoAdministradorInicial { get; set; } = string.Empty;
}
