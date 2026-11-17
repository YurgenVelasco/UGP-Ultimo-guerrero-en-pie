using Microsoft.Data.SqlClient;

namespace UGP_ultimo_guerrero_en_pie_.Services.Implementations;

// Barrera independiente: nunca habilita la creación de compras.
public sealed class WompiWebhookPolicy(IConfiguration configuration, IHostEnvironment environment, WompiCheckout wompi)
{
    public bool Enabled
    {
        get
        {
            if (!configuration.GetValue<bool>("Wompi:Webhooks:Habilitados") ||
                !environment.IsDevelopment() || wompi.EnvironmentName != "Sandbox" || !wompi.EventsConfigured)
                return false;
            try
            {
                var connection = new SqlConnectionStringBuilder(configuration.GetConnectionString("DefaultConnection") ?? "");
                return connection.DataSource == @"(localdb)\MSSQLLocalDB" &&
                    connection.InitialCatalog == "UGP_Integracion_Test" && connection.IntegratedSecurity &&
                    string.IsNullOrEmpty(connection.AttachDBFilename);
            }
            catch (ArgumentException) { return false; }
        }
    }
}
