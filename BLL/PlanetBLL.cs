﻿using StarshipfleetsAPI.DAL;
using StarshipfleetsAPI.Models.Planets;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Web;
using System.Web.SessionState;
using System.Web.UI.WebControls;

namespace StarshipfleetsAPI.BLL
{
    public class PlanetBLL
    {

        public static AllbuildQues GetBuildingQueue(int? PlanetID)
        {
            AllbuildQues allq = new AllbuildQues();
            List<BuildingQue> buildingQues = PlanetDAL.GetBuildingQueue(PlanetID);
            DateTime UTC = DateTime.UtcNow;
            List<BuildingQue> BuildingsQueLeft = new List<BuildingQue>();
            foreach (BuildingQue item in buildingQues)
            {
                if (item.CompletetionDate < UTC)
                {
                    PlanetDAL.InsertUpdatePlanetBuilding(item.PlanetID, item.BuildingID);
                    PlanetDAL.RemoveBuildingQueue(item.BuildQueID);
                }
                else
                {
                    BuildingsQueLeft.Add(item);
                }
            }
            allq.buildingQue = BuildingsQueLeft.OrderBy(x => x.CompletetionDate).ToList();

            return allq;
        }

        public static PlanetDetail AddBuildingQueue(BuildingQue buildingQue)
        {
            List<PlanetBuildings> buildings = PlanetDAL.GetBuildingTypes(buildingQue.PlanetID);
            PlanetBuildings pb = buildings.Find(x => x.BuildingID == buildingQue.BuildingID);            
            PlanetDetail pl = PlanetDAL.GetPlanet(buildingQue.PlanetID, buildingQue.UserID);
            if (pl.Materials < pb.MaterialCost || pl.Population < pb.PopulationCost)
            {
                throw new Exception("Not enough resources");
            }
            else
            {
                PlanetDAL.UpdatePopAndMats(buildingQue.PlanetID, pl.Materials - buildingQue.MaterialCost, pl.Population - (int)pb.PopulationCost.Value);
                pl.Materials = pl.Materials - buildingQue.MaterialCost;
                pl.Population = pl.Population - (int)pb.PopulationCost.Value;
            }

            AllbuildQues BuildingsQueLeft = GetBuildingQueue(buildingQue.PlanetID);
            DateTime UTC = DateTime.UtcNow;

            BuildingQue bq = new BuildingQue();
            bq.BuildingID = buildingQue.BuildingID;
            bq.PlanetID = buildingQue.PlanetID;
            bq.Seconds = buildingQue.Seconds;
            bq.UserID = buildingQue.UserID;

            DateTime? maxCompletetionDate = BuildingsQueLeft.buildingQue.Max(x => x.CompletetionDate);
            if (maxCompletetionDate.HasValue)
            {                
                bq.CompletetionDate = maxCompletetionDate.Value.AddSeconds(buildingQue.Seconds.Value);
            }
            else
            {
                bq.CompletetionDate = UTC.AddSeconds(buildingQue.Seconds.Value);
            }
            PlanetDAL.AddBuildingQue(bq);
            return pl;
        }

        public static PlanetDetail UpdatePlanetHarvest(PlanetDetail planet)
        {
            PlanetDetail pl = PlanetDAL.GetPlanet(planet.PlanetID, planet.Owner);
            if (pl.LastHarvest.HasValue && pl.LastHarvest.Value <= DateTime.UtcNow)
            {
                pl.LastHarvest = PlanetDAL.UpdatePlanetHarvest(planet);
                pl.Materials += planet.Materials;
                pl.Population += planet.Population;                
            }
            else
            {
                throw new Exception("Not time to harvest.");
            }
            return pl;
        }
    }
}