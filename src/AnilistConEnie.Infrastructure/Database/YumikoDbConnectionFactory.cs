namespace AnilistConEnie.Infrastructure.Database;

// La base de Yumiko es otra base, con su propio rol: un tipo propio para que el contenedor pueda
// distinguir las dos conexiones.
public class YumikoDbConnectionFactory(string connectionString) : DbConnectionFactory(connectionString);
