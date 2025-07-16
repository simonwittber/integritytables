namespace IntegrityTables;

public partial class Table<T>
{
    public delegate void ModifyRowDelegate(ref Row<T> row);
    public delegate void BeforeUpdateDelegate(in Row<T> row, ref Row<T> newRow);
    public delegate void AfterUpdateDelegate(in Row<T> row, in Row<T> newRow);
    public delegate void InspectRowDelegate(in Row<T> row);

    // These lists are used to store registered functions for each trigger type.
    public BeforeTriggerList BeforeAdd = new();
    public BeforeUpdateTriggerList BeforeUpdate = new();
    public AfterUpdateTriggerList AfterUpdate = new();
    public AfterTriggerList BeforeRemove = new();
    public AfterTriggerList AfterRemove = new();
    public AfterTriggerList AfterAdd = new();
}