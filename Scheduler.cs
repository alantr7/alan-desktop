using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alan {
    class Scheduler {

        private static List<ScheduledTask> Tasks = new List<ScheduledTask>();

        public static void ScheduleTask(JSONElement Json, long Wait) {
            Tasks.Add(new ScheduledTask(Json, Wait));
        }

        public static void Tick() {
            for (int i = 0; i < Tasks.Count; i++) {
                long Ticks = DateTime.Now.Ticks;
                if (Tasks[i].RunAt - Ticks <= 0) {
                    Server.Respond(null, Tasks[i].Json.c["task"]);
                    i--;
                    Tasks.RemoveAt(i);
                }
            }
        }

    }

    class ScheduledTask {

        public long RunAt = 0;
        public JSONElement Json;

        public ScheduledTask(JSONElement Json, long Wait) {
            this.Json = Json;
            RunAt = DateTime.Now.Ticks + Wait;
        }

    }

}
