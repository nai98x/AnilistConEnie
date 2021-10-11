using System.Collections.Generic;

namespace AnilistESP
{
    public class Media
    {
        public int Id { get; set; }
        public string TituloRomaji { get; set; }
        public string TituloEnglish { get; set; }
        public string UrlAnilist { get; set; }
        public string Descripcion { get; set; }
        public string CoverImage { get; set; }
        public string Estado { get; set; }
        public string Episodios { get; set; }
        public string Chapters { get; set; }
        public string Formato { get; set; }
        public string Score { get; set; }
        public string Fechas { get; set; }
        public string Generos { get; set; }
        public string Tags { get; set; }
        public List<string> Titulos { get; set; }
        public string Estudios { get; set; }
        public string LinksExternos { get; set; }
        public bool IsAdult { get; set; }
        public bool Ok { get; set; }
        public string MsgError { get; set; }
    }
}
