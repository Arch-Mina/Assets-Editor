using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Assets_Editor
{
    public class ScriptData
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string Description { get; set; }
    }

    public class ScriptManager
    {
        private readonly string _scriptsFilePath;
        private List<ScriptData> _scripts;

        public ScriptManager()
        {
            // Store scripts in the executable directory
            var executablePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var executableDir = Path.GetDirectoryName(executablePath);
            _scriptsFilePath = Path.Combine(executableDir, "lua_scripts.json");
            _scripts = new List<ScriptData>();
            LoadScripts();
        }

        public List<ScriptData> Scripts => _scripts.ToList();

        public void LoadScripts()
        {
            try
            {
                if (File.Exists(_scriptsFilePath))
                {
                    var json = File.ReadAllText(_scriptsFilePath);
                    _scripts = JsonConvert.DeserializeObject<List<ScriptData>>(json) ?? new List<ScriptData>();
                }
                else
                {
                    _scripts = new List<ScriptData>();
                    // Create some default scripts
                    CreateDefaultScripts();
                }
            }
            catch (Exception ex)
            {
                _scripts = new List<ScriptData>();
                Console.WriteLine($"Error loading scripts: {ex.Message}");
            }
        }

        public void SaveScripts()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_scripts, Formatting.Indented);
                File.WriteAllText(_scriptsFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving scripts: {ex.Message}");
                throw;
            }
        }

        public void AddScript(ScriptData script)
        {
            if (string.IsNullOrWhiteSpace(script.Name))
                throw new ArgumentException("Script name cannot be empty");

            // Check if script with same name exists
            if (_scripts.Any(s => s.Name.Equals(script.Name, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Script with name '{script.Name}' already exists");

            script.CreatedDate = DateTime.Now;
            script.ModifiedDate = DateTime.Now;
            _scripts.Add(script);
            SaveScripts();
        }

        public void UpdateScript(string scriptName, string newCode, string description = null)
        {
            var script = _scripts.FirstOrDefault(s => s.Name.Equals(scriptName, StringComparison.OrdinalIgnoreCase));
            if (script == null)
                throw new InvalidOperationException($"Script '{scriptName}' not found");

            script.Code = newCode;
            script.ModifiedDate = DateTime.Now;
            if (!string.IsNullOrWhiteSpace(description))
                script.Description = description;

            SaveScripts();
        }

        public void DeleteScript(string scriptName)
        {
            var script = _scripts.FirstOrDefault(s => s.Name.Equals(scriptName, StringComparison.OrdinalIgnoreCase));
            if (script == null)
                throw new InvalidOperationException($"Script '{scriptName}' not found");

            _scripts.Remove(script);
            SaveScripts();
        }

        public ScriptData GetScript(string scriptName)
        {
            return _scripts.FirstOrDefault(s => s.Name.Equals(scriptName, StringComparison.OrdinalIgnoreCase));
        }

        public bool ScriptExists(string scriptName)
        {
            return _scripts.Any(s => s.Name.Equals(scriptName, StringComparison.OrdinalIgnoreCase));
        }

        private void CreateDefaultScripts()
        {
            var defaultScripts = new List<ScriptData>
            {
                new ScriptData
                {
                    Name = "Sample Data Generator",
                    Description = "Generates sample data for testing charts and tables",
                    Code = @"-- Sample Data Generator
function generateSampleData()
    local data = {}
    for i = 1, 20 do
        table.insert(data, {
            Level = i,
            Experience = i * i * 100,
            Health = 100 + (i * 10),
            Mana = 50 + (i * 5),
            Damage = 20 + (i * 2.5),
            Defense = 10 + (i * 1.5)
        })
    end
    return data
end

return generateSampleData()"
                },
                new ScriptData
                {
                    Name = "Weapon Damage Calculator",
                    Description = "Calculates weapon damage based on level and skill",
                    Code = @"-- Weapon Damage Calculator
function calculateWeaponDamage(level, skillLevel, weaponPower)
    local levelWeight = 0.7
    local skillWeight = 1.20
    local powerWeight = 1.0
    local randomFactor = 0.10

    local levelFactor = math.log(level + 10) * levelWeight
    local skillFactor = (skillLevel ^ 1.05) * skillWeight
    local base = levelFactor + skillFactor
    local rawDamage = base * (weaponPower * powerWeight / 6)

    local minDamage = rawDamage * (1 - randomFactor)
    local maxDamage = rawDamage * (1 + randomFactor)
    
    return minDamage, maxDamage
end

-- Generate damage data for levels 1-30
local weaponData = {}
for level = 1, 30 do
    local minDamage, maxDamage = calculateWeaponDamage(level, 5, 1)
    table.insert(weaponData, {
        Level = level,
        SkillLevel = 5,
        WeaponPower = 1,
        MinDamage = math.floor(minDamage * 100) / 100,
        MaxDamage = math.floor(maxDamage * 100) / 100,
        AvgDamage = math.floor(((minDamage + maxDamage) / 2) * 100) / 100
    })
end

return weaponData"
                },
                new ScriptData
                {
                    Name = "Multi-Curve Demo",
                    Description = "Demonstrates multiple curves for dynamic charting",
                    Code = @"-- Multi-Curve Demo for Dynamic Charts
function generateMultiCurveData()
    local data = {}
    
    for i = 1, 25 do
        local x = i
        
        -- Create different growth patterns
        local exponential = 10 + (i * i * 0.3) + math.sin(i * 0.2) * 3
        local logarithmic = 50 + math.log(i + 1) * 8 + math.cos(i * 0.3) * 2
        local linear = 20 + (i * 2.5) + math.sin(i * 0.4) * 4
        local quadratic = 5 + (i * i * 0.2) + math.cos(i * 0.5) * 3
        local oscillating = 30 + math.sin(i * 0.3) * 15 + math.cos(i * 0.1) * 5
        
        table.insert(data, {
            Level = x,
            Exponential = math.floor(exponential * 100) / 100,
            Logarithmic = math.floor(logarithmic * 100) / 100,
            Linear = math.floor(linear * 100) / 100,
            Quadratic = math.floor(quadratic * 100) / 100,
            Oscillating = math.floor(oscillating * 100) / 100
        })
    end
    
    return data
end

return generateMultiCurveData()"
                }
            };

            foreach (var script in defaultScripts)
            {
                _scripts.Add(script);
            }
            
            SaveScripts();
        }
    }
}
