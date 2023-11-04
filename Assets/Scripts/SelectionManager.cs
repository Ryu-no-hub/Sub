using System.Collections.Generic;

public class SelectionManager
{
    private static SelectionManager _instance;

    public static SelectionManager Instance
    {
        get
        {
            if(_instance == null)
            {
                _instance = new SelectionManager();
            }
            return _instance;
        }
        private set
        {
            _instance = value;
        }
    }

    public HashSet<SelectableUnit> SelectedUnits = new HashSet<SelectableUnit>();
    public List<SelectableUnit> AvailableUnits = new List<SelectableUnit>();

    private SelectionManager() { }

    public void Select(SelectableUnit Unit)
    {
        SelectedUnits.Add(Unit);
    }

    public void Deselect(SelectableUnit Unit)
    {
        SelectedUnits.Remove(Unit);
    }

    public void DeselectAll()
    {
        SelectedUnits.Clear();
    }
}
