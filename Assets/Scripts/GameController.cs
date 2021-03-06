using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using Random = UnityEngine.Random;

public class GameController : MonoBehaviour
{
    public TextAsset CsvFile;
    public GameObject boxPrefab;
    public GameObject peakPrefab;
    public GameObject slotPrefab;
    public GameObject warningObject;
    private int validSlotsCount;
    private bool lastBoxPlacedOccupied;
    private List<Slot> highlightedSlots = new List<Slot>();
    private List<Slot> validSlots = new List<Slot>();
    private int occupiedSlotsCount = 0;
    private float xStartCoord = 5.0f;
    private int defaultBoxSize = 4;

    [SerializeField]
    private GameObject scoreObject;
    [SerializeField]
    GameObject PeakContainer;
    public Shader lineShader;

    [Header("Sizes")]
    public float peaksYPos = 15;
    public float boxYPos = 13;
    public float scaleWidth = 0.1f;
    public int rightBoxMarginSpace = 5;
    public int peaksSpace = 5;
    public float slotAndBoxScaling;
    public float slotHeightDivider;

    void OnEnable()
    {
        if (scoreObject == null)
        {
            throw new Exception("score obj. null");
        }

        DrawLine();
        string[] array = CsvFile.text.Split('\n');
        float previousX = 0;
        float xCoord = xStartCoord;
        for (int i = 0; i <= array.Length - 1; i++)
        {
            string[] rows = array[i].Split(',');
            CreatePeakPrefab(xCoord, peaksYPos, 0.2f, 1, xCoord - previousX, i);
            xCoord += peaksSpace;
            previousX = xCoord;
        }
    }

    private void Update()
    {
        Score scoreComponent = scoreObject.GetComponent<Score>();
        scoreComponent.CalculateScore(GetAllBoxes());
        validSlotsCount = validSlots.Count;
    }


    Slot CreateSlotPrefab(float pos_y, float intensity, Peak startPeak, Peak endPeak, bool valid)
    {
        float pos_x1 = startPeak.transform.position.x;
        float pos_x2 = endPeak.transform.position.x;
        if (startPeak == null || endPeak == null)
        {
            throw new NullReferenceException();
        }
        if (pos_x1 == pos_x2)
        {
            throw new ArgumentException();
        }
        GameObject slotObject = Instantiate(slotPrefab, new Vector3(pos_x1, pos_y, 0), Quaternion.identity);
        slotObject.transform.SetParent(GameObject.Find("ValidSlotsContainer").transform);

        Slot slot = slotObject.GetComponent<Slot>();
        slot.startpeak = startPeak;
        slot.endpeak = endPeak;
        slot.SetScale(MathF.Abs(pos_x2 - pos_x1), intensity);

        if (!valid)
        {
            slot.SetColor(new Color32(229, 143, 30, 100));
        }

        return slot;
    }

    Peak CreatePeakPrefab(float pos_x, float pos_y, float scale_x, float scale_y, float width_to_prev, int index)
    {
        GameObject peakObject = Instantiate(peakPrefab, new Vector3(pos_x, pos_y, 0), Quaternion.identity);
        peakObject.transform.SetParent(GameObject.Find("PeakContainer").transform);

        Peak peak = peakObject.GetComponent<Peak>();
        peak.SetImageScale(scale_x * slotAndBoxScaling, scale_y * slotAndBoxScaling);
        peak.SetPos(pos_x * scaleWidth, pos_y);
        peak.index = index;
        return peak;
    }

    DraggableBox CreateBoxPrefab(float pos_x, float pos_y, float scale_x, float scale_y)
    {
        GameObject boxObject = Instantiate(boxPrefab, new Vector3(pos_x, pos_y, 0), Quaternion.identity);
        boxObject.transform.SetParent(GameObject.Find("BoxContainer").transform);
        DraggableBox box = boxObject.GetComponent<DraggableBox>();
        box.gameController = this;
        box.width = scale_x;
        box.SetScale(scale_x * scaleWidth, scale_y * slotAndBoxScaling);
        box.SetPos(pos_x * scaleWidth, pos_y);
        box.posX = pos_x;
        box.SetScoreObject(scoreObject);
        return box;
    }

    internal void HighlightValidSlots(List<int> startIndexes, List<int> endIndexes)
    {
        if (startIndexes.Count != endIndexes.Count)
        {
            throw new Exception("different size of index lists");
        }
        foreach (var startAndEndIndexes in startIndexes.Zip(endIndexes, Tuple.Create))
        {
            Peak startPeak = GetPeak(startAndEndIndexes.Item1);
            Peak endPeak = GetPeak(startAndEndIndexes.Item2);
            float avgIntensity = (startPeak.intensity + endPeak.intensity) / 2;
            if (!SlotOccupied(startPeak.index, endPeak.index, GetAllBoxes()))
            {
                Slot slot = CreateSlotPrefab(peaksYPos, avgIntensity / slotHeightDivider, startPeak, endPeak, true);
                if (slot == null)
                {
                    print("slot in HighlightValidSlots() is null");
                }
                highlightedSlots.Add(slot);
                validSlots.Add(slot);
            }
            else
            {
                Slot slot = CreateSlotPrefab(peaksYPos, avgIntensity / slotHeightDivider, startPeak, endPeak, false);
                highlightedSlots.Add(slot);
            }
        }
    }

    public void ClearSlots()
    {
        highlightedSlots.Clear();
        validSlots.Clear();
        List<Slot> allValidSlots;
        GameObject container = GameObject.Find("ValidSlotsContainer");
        allValidSlots = container.GetComponentsInChildren<Slot>().ToList();

        foreach (Slot slot in allValidSlots)
        {
            Destroy(slot.gameObject);
        }
    }

    /*
    box was placed on a valid slot
    */
    internal Slot BoxPlaced(int score, DraggableBox draggableBox, Peak startpeak)
    {
        Slot selectedSlot = FindMatchingSlot(startpeak, draggableBox);

        draggableBox.SetScale(selectedSlot.GetSlotScaleX(), selectedSlot.GetSlotScaleY());
        draggableBox.placedStartPeak = selectedSlot.startpeak;
        draggableBox.placedEndPeak = selectedSlot.endpeak;

        lastBoxPlacedOccupied = SlotOccupied(selectedSlot.startpeak.index, selectedSlot.endpeak.index, GetAllBoxes());

        if (lastBoxPlacedOccupied)
        {
            HandleInvalidBoxPlacement(draggableBox);
        }
        else
        {
            SpawnNewBox(draggableBox);
        }

        return selectedSlot;
    }

    public void HandleInvalidBoxPlacement(DraggableBox box)
    {
        Warnings warningComponent = warningObject.GetComponent<Warnings>();
        //warningComponent.SetText("You can't place box on top of another box");
        box.ReturnToStartPos();
    }

    /*
    change the scale of the box to the scale of the slot it is placed on
    */
    private Slot FindMatchingSlot(Peak startpeak, DraggableBox draggableBox)
    {
        foreach (Slot slot in highlightedSlots)
        {
            if (slot.startpeak.index == startpeak.index)// && slot.GetWidth() == Int32.Parse(draggableBox.width))
            {
                return slot;
            }
        }
        throw new NullReferenceException("selected slot is null");
    }

    /*
    spawn a new box if there are available slots for that box type
    */
    private void SpawnNewBox(DraggableBox previousBox)
    {
        JSONReader.SerializedSlot[] possibleSlots = previousBox.aminoAcidChar.slots;
        for (int i = 0; i < possibleSlots.Length - 1; i++)
        {
            JSONReader.SerializedSlot serializedSlot = possibleSlots[i];
            int start_peak_index = serializedSlot.start_peak_index;
            int end_peak_index = serializedSlot.end_peak_index;
            if (!SlotOccupied(start_peak_index, end_peak_index, GetAllBoxes()))
            {
                DraggableBox newBox = CreateBox(previousBox.aminoAcidChar, previousBox.posX, previousBox.GetColor());
                //the box always has the same color
                newBox.SetColor(previousBox.GetColor());
                //exit the loop when one new box is created
                return;
            }
        }
    }

    /*
    true if slot between (slotstart, slotend) is occupied by any other box
    */
    public bool SlotOccupied(int slotStart, int slotEnd, DraggableBox[] draggableBoxes)
    {
        for (int i = 0; i < draggableBoxes.Length; i++)
        {
            if (slotStart == null && slotEnd == null)
            {
                throw new NullReferenceException("slot start or end is null in SlotOccupied()");
            }
            DraggableBox box = draggableBoxes[i];
            if (box.GetIsPlaced())
            {
                int boxStart = box.GetStartPeak().index;
                int boxEnd = box.GetEndPeak().index;
                if (boxStart == null && boxEnd == null)
                {
                    throw new NullReferenceException("box start or end is null in SlotOccupied()");
                }

                bool overlaps = Overlap(boxStart, boxEnd, slotStart, slotEnd);
                if (overlaps)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool Overlap(int boxStart, int boxEnd, int slotStart, int slotEnd)
    {
        if (boxStart >= slotEnd)
        {
            return false;
        }
        else if (boxEnd <= slotStart)
        {
            return false;
        }
        return true;
    }

    public DraggableBox[] GetAllBoxes()
    {
        return GameObject.Find("BoxContainer").GetComponentsInChildren<DraggableBox>();
    }

    public Peak GetPeak(int index)
    {
        int count = PeakContainer.transform.childCount;
        if (index > count - 1)
        {
            throw new Exception(index + "higher than total children of " + count);
        }

        GameObject peakIndex = PeakContainer.transform.GetChild(index).gameObject;
        Peak peak = peakIndex.GetComponent<Peak>();

        if (peakIndex == null || peak == null)
        {
            throw new Exception("peak index or peak = null");
        }
        return peak;
    }

    public Peak GetLastPeak()
    {
        int total = PeakContainer.transform.childCount;
        return GetPeak(total - 1);
    }

    public bool GetLastBoxPlacedOccupied()
    {
        return lastBoxPlacedOccupied;
    }

    public int GetValidSlotsCount()
    {
        return validSlotsCount;
    }

    public int GetOccupiedSlotsCount()
    {
        return occupiedSlotsCount;
    }

    public List<Slot> GetValidSlots()
    {
        return validSlots;
    }

    public void CreateAllBoxes(JSONReader.AminoAcid[] aminoAcids)
    {


        int rightBoxMargin = 0;
        int index = 0;
        foreach (JSONReader.AminoAcid aminoAcidChar in aminoAcids)
        {
            validSlotsCount = aminoAcidChar.slots.Length;
            if (aminoAcidChar.slots.Length > 0)
            {
                Color32 color = GetColorFromList(index);

                CreateBox(aminoAcidChar, aminoAcidChar.Mass + rightBoxMargin, color);
                rightBoxMargin += rightBoxMarginSpace;
                index += 1;
                Debug.Log("Created new box: " + aminoAcidChar);
            }
        }
    }

    private Color32 GetColorFromList(int colorIndex)
    {
        Color32[] colors = {
        new Color32(255, 102, 102, 255), new Color32(255, 178, 102, 255), new Color32(255, 255, 102, 255),
        new Color32(178, 255, 102, 255), new Color32(102, 102, 255, 255), new Color32(102, 178, 255, 255),
        new Color32(178, 102, 255, 255), new Color32(255, 102, 178, 255)
        };

        if (colorIndex < colors.Length)
        {
            return colors[colorIndex];
        }
        else
        {
            return new Color32((byte)Random.Range(0, 255), (byte)Random.Range(0, 255), (byte)Random.Range(0, 255), 255);
        }

    }

    private DraggableBox CreateBox(JSONReader.AminoAcid aminoAcidChar, float xPos, Color color)
    {
        DraggableBox box = CreateBoxPrefab(xPos, boxYPos, defaultBoxSize, defaultBoxSize);
        box.aminoAcidChar = aminoAcidChar;
        box.SetColor(color);
        box.SetText(validSlotsCount.ToString() + "/" + aminoAcidChar.slots.Length.ToString());

        foreach (JSONReader.SerializedSlot slot in aminoAcidChar.slots)
        {
            box.startPeakNumbers.Add(slot.start_peak_index);
            box.endPeakNumbers.Add(slot.end_peak_index);
            box.SwitchStartAndEndIndexes();

            //add intensity to peaks
            Peak startPeak = GetPeak(slot.start_peak_index);
            Peak endPeak = GetPeak(slot.end_peak_index);
            float startPeakIntensity = slot.intensity[0];
            float endPeakIntensity = slot.intensity[1];
            startPeak.intensity = startPeakIntensity;
            endPeak.intensity = endPeakIntensity;
            if (slot.start_peak_coord == 0 || slot.end_peak_coord == 0)
            {
                throw new Exception("start or end peak coord is 0");
            }
            startPeak.coord = slot.start_peak_coord;
            endPeak.coord = slot.end_peak_coord;
        }

        return box;
    }

    private void DrawLine()
    {
        LineRenderer l = gameObject.AddComponent<LineRenderer>();
        List<Vector3> pos = new List<Vector3>
        {
            new Vector3(-500, peaksYPos, 0),
            new Vector3(1000, peaksYPos, 0)
        };
        l.material = new Material(lineShader);
        l.startWidth = 0.1f;
        l.SetPositions(pos.ToArray());
        l.useWorldSpace = true;
        l.SetColors(Color.black, Color.black);
    }
}