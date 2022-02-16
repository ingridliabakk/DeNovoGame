using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class GameController : MonoBehaviour
{
    public TextAsset CsvFile;
    public GameObject boxPrefab;
    public GameObject peakPrefab;
    public GameObject slotPrefab;
    private List<Slot> highlightedSlots = new List<Slot>();
    [SerializeField]
    private GameObject scoreObject;

    [Header("Sizes")]
    public float slotAndBoxScaling;
    public float peaksYPos = 15;
    public float boxYPos = 13;
    public float scaleWidth = 0.1f;
    [SerializeField]
    GameObject SlotContainer;

    void Start()
    {
        if (scoreObject == null)
        {
            throw new Exception("score obj. null");
        }

        DrawLine();
        string[] array = CsvFile.text.Split('\n');
        float previousX = 0;
        float xCoord = 5.0f; //= float.Parse(rows[0]);
        for (int i = 0; i <= array.Length - 1; i++)
        {
            string[] rows = array[i].Split(',');

            //float yCoord = float.Parse(rows[1]);

            CreatePeakPrefab(xCoord, peaksYPos, 0.2f, 1, xCoord - previousX, i);
            xCoord += 7;
            previousX = xCoord;
        }
    }

    Slot CreateSlotPrefab(float pos_y, float intensity, Peak startPeak, Peak endPeak)
    {
        print("CreateSlotPrefab");
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

        //TODO: check if this can be removed
        if (pos_x1 > pos_x2)
        {
            slot.SetPos(pos_x2, pos_y);
        }
        else
        {
            slot.SetPos(pos_x1, pos_y);
        }
        return slot;
    }

    Peak CreatePeakPrefab(float pos_x, float pos_y, float scale_x, float scale_y, float width_to_prev, int index)
    {
        GameObject peakObject = Instantiate(peakPrefab, new Vector3(pos_x, pos_y, 0), Quaternion.identity);
        peakObject.transform.SetParent(GameObject.Find("SlotContainer").transform);

        Peak peak = peakObject.GetComponent<Peak>();
        peak.SetImageScale(scale_x * slotAndBoxScaling, scale_y * slotAndBoxScaling);
        peak.SetPos(pos_x * scaleWidth, pos_y);
        peak.SetText(index + "\n" + width_to_prev.ToString());
        peak.index = index;
        return peak;
    }

    DraggableBox CreateBoxPrefab(float pos_x, float pos_y, float scale_x, float scale_y)
    {
        GameObject boxObject = Instantiate(boxPrefab, new Vector3(pos_x, pos_y, 0), Quaternion.identity);
        boxObject.transform.SetParent(GameObject.Find("BoxContainer").transform);
        DraggableBox box = boxObject.GetComponent<DraggableBox>();
        box.width = scale_x.ToString();
        box.SetScale(scale_x * scaleWidth, scale_y * slotAndBoxScaling);
        box.SetPos(pos_x * scaleWidth, pos_y);
        box.posX = pos_x;
        box.SetScoreObject(scoreObject);
        box.SetColor();
        return box;
    }

    internal void HighlightValidSlots(List<int> startIndexes, List<int> endIndexes)
    {
        print("highlightedSlots: ");
        foreach (var startAndEndIndexes in startIndexes.Zip(endIndexes, Tuple.Create))
        {
            Peak startPeak = GetPeak(startAndEndIndexes.Item1);
            Peak endPeak = GetPeak(startAndEndIndexes.Item2);
            if (SlotOccupied(startPeak.index, endPeak.index, GetAllBoxes()))
            {
                //TODO: why return if first slot was occupied? 
                return;
            }
            //draw valid slots, the height is the average intensity of the peaks
            float avgIntensity = (startPeak.intensity + endPeak.intensity) / 2;
            Slot slot = CreateSlotPrefab(peaksYPos, avgIntensity / 10, startPeak, endPeak);
            highlightedSlots.Add(slot);
            print("slot " + slot.ToString());
        }
    }

    public void ClearSlots()
    {
        print("cleares slots");
        highlightedSlots.Clear();
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
    internal Slot BoxPlaced(int score, bool validPosition, DraggableBox draggableBox, Peak startpeak)
    {
        print("highlightedSlots: " + highlightedSlots.Count + " startpeak: " + startpeak);
        Slot selectedSlot = ChangeScaleOfBox(startpeak, draggableBox);
        if (selectedSlot == null)
        {
            throw new NullReferenceException("selected slot is null");
        }
        draggableBox.placedStartPeak = selectedSlot.startpeak;
        draggableBox.placedEndPeak = selectedSlot.endpeak;

        Score scoreComponent = scoreObject.GetComponent<Score>();
        scoreComponent.AddScore(score);
        if (validPosition)
        {
            //spawn new box if there are available slots
            SpawnNewBox(draggableBox);
        }

        return selectedSlot;
    }

    /*
    change the scale of the box to the scale of the slot it is placed on
    */
    private Slot ChangeScaleOfBox(Peak startpeak, DraggableBox draggableBox)
    {
        foreach (Slot slot in highlightedSlots)
        {
            if (slot.startpeak.index == startpeak.index)
            {
                print("set box to slot scale: " + slot.GetSlotScaleX() + " , y: " + slot.GetSlotScaleY());
                draggableBox.SetScale(slot.GetSlotScaleX(), slot.GetSlotScaleY());
                return slot;
            }
        }
        throw new NullReferenceException("selected slot is null");
    }

    /*
    spawn a new box if there are available slots for that box type
    */
    private void SpawnNewBox(DraggableBox draggableBox)
    {
        JSONReader.SerializedSlot[] possibleSlots = draggableBox.aminoAcidChar.slots;
        for (int i = 0; i < possibleSlots.Length; i++)
        {
            print("possibleSlots[i]: " + possibleSlots[i]);
            JSONReader.SerializedSlot serializedSlot = possibleSlots[i];
            int start_peak_index = serializedSlot.start_peak_index;
            int end_peak_index = serializedSlot.end_peak_index;
            if (!SlotOccupied(start_peak_index, end_peak_index, GetAllBoxes()))
            {
                CreateBox(draggableBox.aminoAcidChar, draggableBox.posX);
                continue;
                //TODO: remove from possible slots list  
            }
        }
    }

    /*
    true if slot between (slotstart, slotend) is occupied by any other box
    */
    private bool SlotOccupied(int slotStart, int slotEnd, DraggableBox[] draggableBoxes)
    {
        for (int i = 0; i < draggableBoxes.Length; i++)
        {
            DraggableBox box = draggableBoxes[i];
            if (box.getIsPlaced())
            {
                int boxStart = box.getStartPeak().index;
                int boxEnd = box.getEndPeak().index;
                if (Overlap(boxStart, boxEnd, slotStart, slotEnd))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool Overlap(int boxStart, int boxEnd, int slotStart, int slotEnd)
    {
        if (boxStart < slotEnd && boxEnd > slotStart)
        {
            return true;
        }
        else if (boxStart > slotEnd && boxEnd > slotEnd)
        {
            return true;
        }
        else if (boxStart <= slotEnd && boxEnd <= slotEnd)
        {
            return true;
        }
        return false;
    }

    private DraggableBox[] GetAllBoxes()
    {
        return GameObject.Find("BoxContainer").GetComponentsInChildren<DraggableBox>();
    }

    public Peak GetPeak(int index)
    {
        GameObject peakIndex = SlotContainer.transform.GetChild(index).gameObject;
        Peak peak = peakIndex.GetComponent<Peak>();

        if (peakIndex == null || peak == null)
        {
            throw new Exception("peak index or peak = null");
        }
        return peak;
    }

    public void CreateAllBoxes(JSONReader.AminoAcid[] aminoAcids)
    {
        int rightBoxMargin = 0;
        foreach (JSONReader.AminoAcid aminoAcidChar in aminoAcids)
        {
            if (aminoAcidChar.slots.Length > 0)
            {
                CreateBox(aminoAcidChar, aminoAcidChar.Mass + rightBoxMargin);
                rightBoxMargin += 12;
            }
        }
    }

    private void CreateBox(JSONReader.AminoAcid aminoAcidChar, float xPos)
    {
        DraggableBox box = CreateBoxPrefab(xPos, boxYPos, aminoAcidChar.Mass, aminoAcidChar.Mass);

        foreach (JSONReader.SerializedSlot slot in aminoAcidChar.slots)
        {
            box.startPeakNumbers.Add(slot.start_peak_index);
            box.endPeakNumbers.Add(slot.end_peak_index);
            box.SwitchStartAndEndIndexes();
            box.aminoAcidChar = aminoAcidChar;
            //add intensity to peaks
            Peak startPeak = GetPeak(slot.start_peak_index);
            Peak endPeak = GetPeak(slot.end_peak_index);
            float startPeakIntensity = slot.intensity[0];
            float endPeakIntensity = slot.intensity[1];
            startPeak.intensity = startPeakIntensity;
            endPeak.intensity = endPeakIntensity;
        }
    }

    private void DrawLine()
    {
        LineRenderer l = gameObject.AddComponent<LineRenderer>();
        List<Vector3> pos = new List<Vector3>
        {
            new Vector3(-500, peaksYPos, 0),
            new Vector3(1000, peaksYPos, 0)
        };

        l.startWidth = 0.1f;
        l.SetPositions(pos.ToArray());
        l.useWorldSpace = true;
    }

}