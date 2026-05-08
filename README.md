# MedicalCenter

## `launch.cmd` y `LAUNCH_ON_RUNNING`

El script `launch.cmd` detecta si `MedicalCenter.Launcher.exe` ya esta activo antes de compilar.

- Sin variable definida: modo por defecto `prompt` (pregunta `Y/N`).
- `LAUNCH_ON_RUNNING=abort`: aborta con salida `10` si detecta instancia activa.
- `LAUNCH_ON_RUNNING=kill`: intenta cerrar procesos activos y continua; si falla sale con `12` (kill) o `13` (revalidacion post-kill).
- En modo `prompt`, responder `N` devuelve salida `11`.