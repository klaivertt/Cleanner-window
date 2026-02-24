using System;
using System.Collections.Generic;

namespace NettoyerPc.Core
{
    public class CleaningStep
    {
        public string Name { get; set; } = string.Empty;
        /// <summary>Catégorie pour la sélection granulaire (ex: "temp","dev","gaming","thirdparty","sysopt","network")</summary>
        public string Category { get; set; } = "general";
        public string Status { get; set; } = "En attente";
        public int Progress { get; set; } = 0;
        public bool IsCompleted { get; set; } = false;
        public bool HasError { get; set; } = false;
        public string? ErrorMessage { get; set; }
        public TimeSpan Duration { get; set; }
        public int FilesDeleted { get; set; } = 0;
        public long SpaceFreed { get; set; } = 0;

        /// <summary>Journal d'activité détaillé de l'étape (rempli par le module pendant l'exécution).</summary>
        public List<string> Logs { get; } = new();

        /// <summary>Ajoute une entrée au journal et met à jour le statut visible.</summary>
        public void AddLog(string msg)
        {
            lock (Logs) Logs.Add(msg);
            Status = msg;
        }
    }
}
