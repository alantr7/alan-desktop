using SuperWebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Alan.command.Command;

namespace Alan.command {
    class cmdHelp {

        public static string Answer(WebSocketSession client, string line) {
            string[] answer = new string[] {
                "help\t\tLista svih komandi",
                "ip\t\t\tProvjeri IP adrese racunara",
                "http\t\tIspise odgovor stranice na HTTP zahtjev",
                "onedrive\t\tUploaduj ili preuzmi fajl sa OneDrive-a",
                "app\t\tPreuzmi i instaliraj aplikacije",
                "file\t\tUpravljaj fajlovima",
                "encoder\t\tEnkriptuj ili dekriptuj string",
                "desktop\t\tPovezi se na drugi racunar",
                "ping\t\tProvjeri dostupnost stranice i vrijeme odgovora",
                "process\t\tPokreni, unisti ili vidi detalje o procesu",
                "var\t\tDefinisi novu varijablu ili vidi listu postojecih",
                "clear\t\tVrati terminal u pocetno stanje"
            };

            foreach (string a in answer)
                client.Send(a);

            return "exit\t\tUgasi program";

        }

    }
}
