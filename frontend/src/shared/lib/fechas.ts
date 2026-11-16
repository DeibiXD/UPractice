/** Fecha de hoy en Honduras como "YYYY-MM-DD" (para validar "posterior a hoy"). */
export function hoyHonduras(): string {
  return new Intl.DateTimeFormat('en-CA', { timeZone: 'America/Tegucigalpa' }).format(new Date())
}
