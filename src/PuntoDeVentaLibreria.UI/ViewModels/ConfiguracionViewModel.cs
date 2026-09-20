using System.Collections.ObjectModel;
using System.Printing;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PuntoDeVentaLibreria.Application.DTOs.Backup;
using PuntoDeVentaLibreria.Application.DTOs.Configuracion;
using PuntoDeVentaLibreria.Application.DTOs.Peripherals;
using PuntoDeVentaLibreria.Application.DTOs.Seguridad;
using PuntoDeVentaLibreria.Application.DTOs.Sistema;
using PuntoDeVentaLibreria.Application.DTOs.Ventas;
using PuntoDeVentaLibreria.Application.Services;
using PuntoDeVentaLibreria.Domain.Entities.Seguridad;
using Wpf.Ui.Appearance;

namespace PuntoDeVentaLibreria.UI.ViewModels;

public partial class ConfiguracionViewModel : ObservableObject
{
    private readonly IConfiguracionService _configuracionService;
    private readonly ILicenseService _licenseService;
    private readonly IUpdateService _updateService;
    private readonly IBackupService _backupService;
    private readonly ITicketPrinterService _ticketPrinterService;
    private readonly IDatabaseMigrationService _databaseMigrationService;
    private readonly IAuthService _authService;

    [ObservableProperty]
    private ConfiguracionNegocioDto _config = new();

    [ObservableProperty]
    private EstadoRetirosDueñoDto _estadoRetiros = new();

    [ObservableProperty]
    private EstadoLicenciaDto _estadoLicencia = new();

    [ObservableProperty]
    private string _claveActivacionInput = string.Empty;

    [ObservableProperty]
    private string _mensajeActivacion = string.Empty;

    [ObservableProperty]
    private ActualizacionDto _actualizacionInfo = new();

    [ObservableProperty]
    private string _mensajeActualizacion = string.Empty;

    [ObservableProperty]
    private bool _estaDescargandoActualizacion;

    [ObservableProperty]
    private double _progresoDescarga;

    [ObservableProperty]
    private string _mensajeBackup = string.Empty;

    [ObservableProperty]
    private string _mensajeGuardado = string.Empty;

    [ObservableProperty]
    private string _mensajeConexionDb = string.Empty;

    [ObservableProperty]
    private bool _estaProbandoConexionDb;

    [ObservableProperty]
    private bool _estaMigrandoBaseDatos;

    [ObservableProperty]
    private string _progresoMigracionTexto = string.Empty;

    // === GESTIÓN DE USUARIOS Y SEGURIDAD ===
    [ObservableProperty]
    private SesionUsuarioDto? _sesionActual;

    public bool EsAdmin => SesionActual?.EsAdmin ?? true;

    [ObservableProperty]
    private string _passwordActual = string.Empty;

    [ObservableProperty]
    private string _passwordNueva = string.Empty;

    [ObservableProperty]
    private string _passwordNuevaConfirmacion = string.Empty;

    [ObservableProperty]
    private string _mensajePassword = string.Empty;

    [ObservableProperty]
    private bool _mensajePasswordExito;

    // Nuevo Usuario Form
    [ObservableProperty]
    private string _nuevoUsername = string.Empty;

    [ObservableProperty]
    private string _nuevoNombreCompleto = string.Empty;

    [ObservableProperty]
    private string _nuevoPasswordUsuario = string.Empty;

    [ObservableProperty]
    private RolUsuario _nuevoRolUsuario = RolUsuario.Vendedor;

    [ObservableProperty]
    private string _mensajeUsuarioCrud = string.Empty;

    [ObservableProperty]
    private bool _mensajeUsuarioCrudExito;

    public RolUsuario[] RolesDisponibles => new[] { RolUsuario.Vendedor, RolUsuario.Encargado, RolUsuario.Administrador };

    public ObservableCollection<UsuarioDto> Usuarios { get; } = new();

    public bool EsPapel80Mm
    {
        get => Config.AnchoPapelMm == 80;
        set
        {
            if (value)
            {
                Config.AnchoPapelMm = 80;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EsPapel58Mm));
            }
        }
    }

    public bool EsPapel58Mm
    {
        get => Config.AnchoPapelMm == 58;
        set
        {
            if (value)
            {
                Config.AnchoPapelMm = 58;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EsPapel80Mm));
            }
        }
    }

    public bool EsModoPostgres
    {
        get => Config.MotorBaseDatos.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase);
        set
        {
            Config.MotorBaseDatos = value ? "PostgreSQL" : "SQLite";
            OnPropertyChanged();
            OnPropertyChanged(nameof(EsModoSqlite));
        }
    }

    public bool EsModoSqlite
    {
        get => !EsModoPostgres;
        set
        {
            Config.MotorBaseDatos = value ? "SQLite" : "PostgreSQL";
            OnPropertyChanged();
            OnPropertyChanged(nameof(EsModoPostgres));
        }
    }

    public bool EsCajaIndependiente
    {
        get => Config.ModoCajaMultiTerminal.Equals("Independiente", StringComparison.OrdinalIgnoreCase);
        set
        {
            Config.ModoCajaMultiTerminal = value ? "Independiente" : "Compartida";
            OnPropertyChanged();
            OnPropertyChanged(nameof(EsCajaCompartida));
        }
    }

    public bool EsCajaCompartida
    {
        get => !EsCajaIndependiente;
        set
        {
            Config.ModoCajaMultiTerminal = value ? "Compartida" : "Independiente";
            OnPropertyChanged();
            OnPropertyChanged(nameof(EsCajaIndependiente));
        }
    }

    public ObservableCollection<string> ImpresorasDisponibles { get; } = new();
    public ObservableCollection<BackupInfoDto> HistorialBackups { get; } = new();
    public ObservableCollection<string> PaisesDisponibles { get; } = new()
    {
        "Argentina",
        "Chile",
        "Uruguay",
        "Estados Unidos",
        "España / Europa",
        "México",
        "Colombia",
        "Personalizado"
    };

    public Func<string, string, int, string, Task>? SolicitarVistaPreviaTicket { get; set; }
    public event Action? OnConfiguracionGuardada;

    public ConfiguracionViewModel(
        IConfiguracionService configuracionService,
        ILicenseService licenseService,
        IUpdateService updateService,
        IBackupService backupService,
        ITicketPrinterService ticketPrinterService,
        IDatabaseMigrationService databaseMigrationService,
        IAuthService authService)
    {
        _configuracionService = configuracionService ?? throw new ArgumentNullException(nameof(configuracionService));
        _licenseService = licenseService ?? throw new ArgumentNullException(nameof(licenseService));
        _updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));
        _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
        _ticketPrinterService = ticketPrinterService ?? throw new ArgumentNullException(nameof(ticketPrinterService));
        _databaseMigrationService = databaseMigrationService ?? throw new ArgumentNullException(nameof(databaseMigrationService));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    public void EstablecerSesion(SesionUsuarioDto sesion)
    {
        SesionActual = sesion;
        OnPropertyChanged(nameof(EsAdmin));
    }


    [RelayCommand]
    public void SeleccionarPais(string? pais)
    {
        if (string.IsNullOrWhiteSpace(pais)) return;
        Config.Pais = pais;

        switch (pais)
        {
            case "Argentina":
                Config.SimboloMoneda = "$";
                Config.BilletesHabilitados = "100,200,500,1000,2000,10000,20000";
                break;
            case "Chile":
                Config.SimboloMoneda = "$";
                Config.BilletesHabilitados = "1000,2000,5000,10000,20000";
                break;
            case "Uruguay":
                Config.SimboloMoneda = "$";
                Config.BilletesHabilitados = "20,50,100,200,500,1000,2000";
                break;
            case "Estados Unidos":
                Config.SimboloMoneda = "US$";
                Config.BilletesHabilitados = "1,2,5,10,20,50,100";
                break;
            case "España / Europa":
                Config.SimboloMoneda = "€";
                Config.BilletesHabilitados = "5,10,20,50,100,200";
                break;
            case "México":
                Config.SimboloMoneda = "$";
                Config.BilletesHabilitados = "20,50,100,200,500,1000";
                break;
            case "Colombia":
                Config.SimboloMoneda = "$";
                Config.BilletesHabilitados = "2000,5000,10000,20000,50000,100000";
                break;
        }

        OnPropertyChanged(nameof(Config));
    }

    public async Task CargarDatosAsync()
    {
        Config = await _configuracionService.ObtenerConfiguracionAsync();
        EstadoRetiros = await _configuracionService.ObtenerEstadoRetirosDueñoMesAsync();
        EstadoLicencia = await _licenseService.ValidarLicenciaAsync();

        // Cargar impresoras instaladas en Windows
        ImpresorasDisponibles.Clear();
        try
        {
            var server = new LocalPrintServer();
            foreach (var q in server.GetPrintQueues())
            {
                ImpresorasDisponibles.Add(q.Name);
            }
        }
        catch { }

        await CargarHistorialBackupsAsync();
        await CargarUsuariosAsync();
        ActualizacionInfo.VersionActual = _updateService.ObtenerVersionActual();
        OnPropertyChanged(nameof(ActualizacionInfo));
        OnPropertyChanged(nameof(EsPapel80Mm));
        OnPropertyChanged(nameof(EsPapel58Mm));
    }

    private async Task CargarHistorialBackupsAsync()
    {
        HistorialBackups.Clear();
        var lista = await _backupService.ObtenerHistorialBackupsAsync();
        foreach (var b in lista)
        {
            HistorialBackups.Add(b);
        }
    }

    [RelayCommand]
    private void CargarLogo()
    {
        try
        {
            var ofd = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Seleccionar Logo del Comercio",
                Filter = "Archivos de Imagen (*.png;*.jpg;*.jpeg;*.bmp;*.webp)|*.png;*.jpg;*.jpeg;*.bmp;*.webp|Todos los archivos (*.*)|*.*",
                Multiselect = false
            };

            if (ofd.ShowDialog() == true)
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var logosDir = System.IO.Path.Combine(baseDir, "Logos");
                if (!System.IO.Directory.Exists(logosDir))
                {
                    System.IO.Directory.CreateDirectory(logosDir);
                }

                var ext = System.IO.Path.GetExtension(ofd.FileName);
                var destPath = System.IO.Path.Combine(logosDir, $"logo_comercio{ext}");
                System.IO.File.Copy(ofd.FileName, destPath, true);

                Config.LogoRuta = destPath;
                OnPropertyChanged(nameof(Config));
                MensajeGuardado = "Logo seleccionado correctamente. Guarde los cambios para aplicar.";
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error al cargar el logo: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void QuitarLogo()
    {
        Config.LogoRuta = null;
        OnPropertyChanged(nameof(Config));
        MensajeGuardado = "Logo removido. Guarde los cambios para aplicar.";
    }

    [RelayCommand]
    private async Task GuardarConfiguracionAsync()
    {
        await _configuracionService.GuardarConfiguracionAsync(Config);
        MensajeGuardado = "¡Configuración guardada exitosamente!";
        OnConfiguracionGuardada?.Invoke();
    }

    [RelayCommand]
    private void CambiarTema(string? tema)
    {
        if (string.Equals(tema, "Dark", StringComparison.OrdinalIgnoreCase))
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Dark);
            Config.TemaInterfaz = "Dark";
        }
        else
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Light);
            Config.TemaInterfaz = "Light";
        }
    }

    [RelayCommand]
    private async Task ActivarLicenciaAsync()
    {
        var res = await _licenseService.ActivarLicenciaAsync(ClaveActivacionInput);
        MensajeActivacion = res.Mensaje;
        EstadoLicencia = await _licenseService.ValidarLicenciaAsync();
        ClaveActivacionInput = string.Empty;
    }

    [RelayCommand]
    private async Task BuscarActualizacionesAsync()
    {
        MensajeActualizacion = "Consultando versiones oficiales disponibles...";
        var res = await _updateService.VerificarActualizacionesAsync(Config.GitHubRepoOwner, Config.GitHubRepoName);
        ActualizacionInfo = res;

        if (res.HayActualizacion)
        {
            MensajeActualizacion = $"¡Nueva versión v{res.VersionDisponible} disponible!";
        }
        else
        {
            MensajeActualizacion = $"Tiene instalada la versión más reciente (v{res.VersionActual}).";
        }
    }

    [RelayCommand]
    private void AbrirVentanaActualizacion()
    {
        if (ActualizacionInfo == null || !ActualizacionInfo.HayActualizacion) return;
        var modal = new Views.ActualizacionModalWindow(ActualizacionInfo, _updateService);
        modal.Owner = System.Windows.Application.Current.MainWindow;
        modal.ShowDialog();
    }

    [RelayCommand]
    private async Task DescargarEInstalarActualizacionAsync()
    {
        if (!ActualizacionInfo.HayActualizacion || string.IsNullOrEmpty(ActualizacionInfo.UrlDescargaZip))
            return;

        EstaDescargandoActualizacion = true;
        MensajeActualizacion = "Descargando actualización oficial...";

        try
        {
            var progreso = new Progress<ProgresoDescargaDto>(p =>
            {
                ProgresoDescarga = p.Porcentaje;
                MensajeActualizacion = p.Mensaje;
            });

            var rutaZip = await _updateService.DescargarActualizacionAsync(ActualizacionInfo, progreso);
            MensajeActualizacion = "Instalando y reiniciando sistema...";
            _updateService.IniciarInstalacion(rutaZip);
        }
        catch (Exception ex)
        {
            MensajeActualizacion = $"Error en la actualización: {ex.Message}";
        }
        finally
        {
            EstaDescargandoActualizacion = false;
        }
    }

    [RelayCommand]
    private async Task CrearBackupAhoraAsync()
    {
        try
        {
            var b = await _backupService.CrearBackupAsync(null, "manual");
            MensajeBackup = $"¡Copia de seguridad creada con éxito!\nArchivo: {b.NombreArchivo} ({b.TamañoFormateado})";
            await CargarHistorialBackupsAsync();
        }
        catch (Exception ex)
        {
            MensajeBackup = $"Error al crear copia de seguridad:\n{ex.Message}";
        }
    }

    [RelayCommand]
    private void SeleccionarCarpetaBackups()
    {
        try
        {
            var ofd = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Seleccionar Carpeta para Copias de Seguridad (Backups)",
                Multiselect = false
            };

            if (ofd.ShowDialog() == true)
            {
                Config.CarpetaBackupsPersonalizada = ofd.FolderName;
                OnPropertyChanged(nameof(Config));
                MensajeBackup = $"Carpeta de destino actualizada a: {ofd.FolderName}. Recuerde guardar la configuración.";
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error al seleccionar carpeta: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void AbrirCarpetaBackups()
    {
        try
        {
            var carpeta = _backupService.ObtenerCarpetaBackupsPredeterminada();
            if (System.IO.Directory.Exists(carpeta))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = carpeta,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"No se pudo abrir la carpeta: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task RestaurarBackupAsync(BackupInfoDto? backup)
    {
        if (backup == null) return;

        var confirm = System.Windows.MessageBox.Show(
            $"ATENCIÓN: ¿Está seguro que desea restaurar la copia de seguridad:\n'{backup.NombreArchivo}'?\n\nEsta acción reemplazará los datos actuales por los datos de este respaldo. Se creará una copia preventiva automática antes de restaurar.",
            "Confirmar Restauración de Copia de Seguridad",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (confirm != System.Windows.MessageBoxResult.Yes) return;

        try
        {
            var ok = await _backupService.RestaurarBackupAsync(backup.RutaCompleta);
            if (ok)
            {
                System.Windows.MessageBox.Show(
                    "¡Copia de seguridad restaurada correctamente!\n\nPor favor, reinicie la aplicación para que todos los módulos recarguen los datos restaurados.",
                    "Restauración Completada",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                await CargarHistorialBackupsAsync();
            }
            else
            {
                System.Windows.MessageBox.Show("No se pudo restaurar el archivo de respaldo seleccionado.", "Error de Restauración", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error al restaurar: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task EliminarBackupAsync(BackupInfoDto? backup)
    {
        if (backup == null) return;

        var confirm = System.Windows.MessageBox.Show(
            $"¿Desea eliminar permanentemente el archivo de copia de seguridad '{backup.NombreArchivo}'?",
            "Eliminar Backup",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (confirm != System.Windows.MessageBoxResult.Yes) return;

        await _backupService.EliminarBackupAsync(backup.RutaCompleta);
        await CargarHistorialBackupsAsync();
        MensajeBackup = "Copia de seguridad eliminada.";
    }

    [RelayCommand]
    private async Task ProbarConexionPostgresAsync()
    {
        EstaProbandoConexionDb = true;
        MensajeConexionDb = "Probando conexión con el servidor PostgreSQL...";

        try
        {
            var dbConfig = new PuntoDeVentaLibreria.Infrastructure.Data.DatabaseConfig
            {
                Motor = "PostgreSQL",
                ServidorPostgres = Config.ServidorPostgres,
                PuertoPostgres = Config.PuertoPostgres,
                BaseDatosPostgres = Config.BaseDatosPostgres,
                UsuarioPostgres = Config.UsuarioPostgres,
                PasswordPostgres = Config.PasswordPostgres,
                NombreTerminal = Config.NombreTerminal,
                ModoCajaMultiTerminal = Config.ModoCajaMultiTerminal
            };

            var (exito, mensaje) = await PuntoDeVentaLibreria.Infrastructure.Data.DatabaseConfigProvider.ProbarConexionPostgresAsync(dbConfig);
            MensajeConexionDb = mensaje;
            if (exito)
            {
                // Guardar la configuración en database_config.json local
                PuntoDeVentaLibreria.Infrastructure.Data.DatabaseConfigProvider.Guardar(dbConfig);
            }
        }
        catch (Exception ex)
        {
            MensajeConexionDb = $"Error al probar conexión: {ex.Message}";
        }
        finally
        {
            EstaProbandoConexionDb = false;
        }
    }

    [RelayCommand]
    private async Task MigrarSqliteAPostgresAsync()
    {
        var confirm = System.Windows.MessageBox.Show(
            "¿Desea migrar todos los datos locales (artículos, clientes, proveedores, ventas e historial) a la base de datos PostgreSQL en la nube?\n\nEste proceso creará las tablas necesarias en el servidor y cargará todos los registros sin alterar la base local.",
            "Confirmar Migración a la Nube",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (confirm != System.Windows.MessageBoxResult.Yes) return;

        EstaMigrandoBaseDatos = true;
        ProgresoMigracionTexto = "Iniciando proceso de migración...";

        try
        {
            var dbConfig = new PuntoDeVentaLibreria.Infrastructure.Data.DatabaseConfig
            {
                Motor = "PostgreSQL",
                ServidorPostgres = Config.ServidorPostgres,
                PuertoPostgres = Config.PuertoPostgres,
                BaseDatosPostgres = Config.BaseDatosPostgres,
                UsuarioPostgres = Config.UsuarioPostgres,
                PasswordPostgres = Config.PasswordPostgres,
                NombreTerminal = Config.NombreTerminal,
                ModoCajaMultiTerminal = Config.ModoCajaMultiTerminal
            };

            var connStr = PuntoDeVentaLibreria.Infrastructure.Data.DatabaseConfigProvider.ObtenerCadenaConexion(dbConfig);
            var progreso = new Progress<string>(msg => ProgresoMigracionTexto = msg);

            var resultado = await _databaseMigrationService.MigrarSqliteAPostgresAsync(connStr, progreso);
            if (resultado.Exitoso)
            {
                PuntoDeVentaLibreria.Infrastructure.Data.DatabaseConfigProvider.Guardar(dbConfig);
                System.Windows.MessageBox.Show(
                    $"{resultado.Mensaje}\n\nPara comenzar a operar en la nube con todas las terminales, reinicie la aplicación.",
                    "¡Migración Exitosa!",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            else
            {
                System.Windows.MessageBox.Show(resultado.Mensaje, "Error en Migración", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error: {ex.Message}", "Error en Migración", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            EstaMigrandoBaseDatos = false;
        }
    }


    [RelayCommand]
    private async Task ProbarVistaPreviaTicketAsync()
    {
        var ventaDemo = CrearVentaDemo();
        var configTicket = CrearConfigTicket();

        var textoTicket = await _ticketPrinterService.GenerarTicketTextoAsync(ventaDemo, configTicket);

        if (SolicitarVistaPreviaTicket != null)
        {
            await SolicitarVistaPreviaTicket(textoTicket, ventaDemo.NumeroComprobante, Config.AnchoPapelMm, Config.ImpresoraTickets);
        }
    }

    [RelayCommand]
    private async Task ProbarTicketAsync()
    {
        var ventaDemo = CrearVentaDemo();
        var configTicket = CrearConfigTicket();

        await _ticketPrinterService.ImprimirTicketVentaAsync(ventaDemo, configTicket);
        MensajeGuardado = "Ticket de prueba generado y guardado en la carpeta ./Tickets/";
    }

    private VentaRealizadaDto CrearVentaDemo()
    {
        return new VentaRealizadaDto
        {
            NumeroComprobante = "L-DEMO-0012",
            Fecha = DateTime.Now,
            SubtotalBruto = 6820m,
            TotalCobrado = 6820m,
            MontoEntregado = 10000m,
            Vuelto = 3180m,
            MetodoPago = "Efectivo",
            ClienteNombre = "Consumidor Final",
            VendedoraNombre = Config.VendedoraDefecto,
            Lineas = new List<ItemCarritoDto>
            {
                new() { Descripcion = "Cuaderno Tapa Dura Rivadavia 48H Rayado", Cantidad = 1, PrecioUnitario = 3520m },
                new() { Descripcion = "Bolígrafo BIC Cristal Azul 1.0mm", Cantidad = 3, PrecioUnitario = 600m },
                new() { Descripcion = "Resaltador Pelikan Fluo Amarillo", Cantidad = 1, PrecioUnitario = 1500m }
            }
        };
    }

    private ConfiguracionTicketDto CrearConfigTicket()
    {
        return new ConfiguracionTicketDto
        {
            NombreComercio = Config.NombreComercio,
            Direccion = Config.Direccion,
            Telefono = Config.Telefono,
            Cuit = Config.Cuit,
            AnchoPapelMm = Config.AnchoPapelMm,
            ImpresoraNombre = Config.ImpresoraTickets,
            MensajePie = Config.MensajePieTicket
        };
    }

    // =========================================================================
    // USUARIOS, CLAVES Y CONTACTO SOPORTE WHATSAPP
    // =========================================================================

    [RelayCommand]
    public async Task CargarUsuariosAsync()
    {
        Usuarios.Clear();
        var lista = await _authService.ObtenerUsuariosActivosAsync();
        foreach (var u in lista)
        {
            Usuarios.Add(u);
        }
    }

    [RelayCommand]
    private async Task CambiarMiPasswordAsync()
    {
        MensajePassword = string.Empty;

        if (SesionActual == null)
        {
            MensajePassword = "No hay sesión activa.";
            MensajePasswordExito = false;
            return;
        }

        if (string.IsNullOrWhiteSpace(PasswordActual) || string.IsNullOrWhiteSpace(PasswordNueva))
        {
            MensajePassword = "Debe ingresar su contraseña actual y la nueva contraseña.";
            MensajePasswordExito = false;
            return;
        }

        if (PasswordNueva != PasswordNuevaConfirmacion)
        {
            MensajePassword = "La nueva contraseña y su confirmación no coinciden.";
            MensajePasswordExito = false;
            return;
        }

        var res = await _authService.CambiarPasswordAsync(SesionActual.UsuarioId, PasswordActual, PasswordNueva);
        MensajePassword = res.Mensaje;
        MensajePasswordExito = res.Exitoso;

        if (res.Exitoso)
        {
            PasswordActual = string.Empty;
            PasswordNueva = string.Empty;
            PasswordNuevaConfirmacion = string.Empty;
        }
    }

    [RelayCommand]
    private async Task CrearNuevoUsuarioAsync()
    {
        MensajeUsuarioCrud = string.Empty;

        if (string.IsNullOrWhiteSpace(NuevoUsername) || string.IsNullOrWhiteSpace(NuevoNombreCompleto) || string.IsNullOrWhiteSpace(NuevoPasswordUsuario))
        {
            MensajeUsuarioCrud = "Debe completar usuario, nombre y contraseña.";
            MensajeUsuarioCrudExito = false;
            return;
        }

        var dto = new CrearUsuarioDto
        {
            Username = NuevoUsername,
            NombreCompleto = NuevoNombreCompleto,
            Password = NuevoPasswordUsuario,
            Rol = NuevoRolUsuario
        };

        var res = await _authService.CrearUsuarioAsync(dto);
        MensajeUsuarioCrud = res.Mensaje;
        MensajeUsuarioCrudExito = res.Exitoso;

        if (res.Exitoso)
        {
            NuevoUsername = string.Empty;
            NuevoNombreCompleto = string.Empty;
            NuevoPasswordUsuario = string.Empty;
            NuevoRolUsuario = RolUsuario.Vendedor;
            await CargarUsuariosAsync();
        }
    }

    [RelayCommand]
    private async Task EliminarUsuarioAsync(UsuarioDto? usuario)
    {
        if (usuario == null) return;
        if (SesionActual == null) return;

        var confirm = System.Windows.MessageBox.Show(
            $"¿Confirma desactivar el usuario '{usuario.Username}' ({usuario.NombreCompleto})?",
            "Confirmar Desactivación de Usuario",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (confirm != System.Windows.MessageBoxResult.Yes) return;

        var res = await _authService.EliminarODesactivarUsuarioAsync(usuario.Id, SesionActual.UsuarioId);
        MensajeUsuarioCrud = res.Mensaje;
        MensajeUsuarioCrudExito = res.Exitoso;

        if (res.Exitoso)
        {
            await CargarUsuariosAsync();
        }
        else
        {
            System.Windows.MessageBox.Show(res.Mensaje, "MR SYS Seguridad", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private void AbrirWhatsAppSoporte()
    {
        try
        {
            var url = "https://wa.me/5493493495801?text=Hola,%20quisiera%20activar/renovar%20el%20Plan%20PRO%20Multi-Terminal%20de%20MR%20SYS%20Librer%C3%ADa";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Contacto de Soporte:\nWhatsApp: +54 9 3493 495801\n\n(No se pudo abrir el navegador: {ex.Message})", "MR SYS Soporte", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
    }
}
