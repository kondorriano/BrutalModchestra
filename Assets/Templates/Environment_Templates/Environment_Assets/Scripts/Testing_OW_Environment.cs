using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Testing_OW_Environment : MonoBehaviour
{
    #region EDITOR EXPOSED FIELDS
    [Header("Overworld Environment Data")]
    public OverworldEnvironmentData m_OWEnvData;
    #endregion

    #region FIELDS
    List<SpriteRenderer> _idleProps = new List<SpriteRenderer>();
    List<OverworldPropLineHandler> _propLines = new List<OverworldPropLineHandler>();
    #endregion

    private void Start()
    {
        LookForData();
        InitializeOWEnvironment();
    }

    #region Initialization
    void LookForData()
    {
        if (m_OWEnvData == null)
        {
            m_OWEnvData = GameObject.FindObjectOfType<OverworldEnvironmentData>();
            if (m_OWEnvData == null)
                Debug.LogWarning("HEY!, Remember to drag your environment prefab into the scene and to set up the OverworldEnvironmentData!");
        }
    }
    public void InitializeOWEnvironment()
    {
        //Initialize extra
        //Character Visuals Set Up
        //Environment Props
        PrepareNewProps();
    }

    /*
    void InitializeCharacterManagerPosition(OverworldCharactersManager visuals)
    {
        CharacterVisuals = visuals;
        CharacterVisuals.AlignToWorldPosition(m_OWEnvData.m_WorldGround, m_OWEnvData.m_CharacterOffsetAngle, m_OWEnvData.m_HeightFromWorldGroundCenter);
    }
    */
    void PrepareNewProps()
    {
        if (!m_OWEnvData.HasPropData)
            return;
        float angleSize = Mathf.Abs(m_OWEnvData.m_AngleLimits.x - m_OWEnvData.m_AngleLimits.y);
        int angleCount = Mathf.CeilToInt(angleSize / m_OWEnvData.m_PropLineAngle);

        float angle;
        float sideOffset = m_OWEnvData.m_PropSideOffset * m_OWEnvData.m_PropWidth;
        float angleOffset = m_OWEnvData.m_PropAngleOffset * m_OWEnvData.m_PropLineAngle;
        //iterate over angles
        for (int i = 0; i < angleCount; i++)
        {
            //Center angle Pos
            angle = m_OWEnvData.m_AngleLimits.y + m_OWEnvData.m_PropLineAngle * i;

            OverworldPropLineHandler lineHandler = GenerateLinePropHandler();
            PreparePropLine(lineHandler, angle, sideOffset, angleOffset);
            _propLines.Add(lineHandler);
        }
    }
    #endregion

    #region Transition
    bool movingWorld = false;
    public void OnMoveWorldClicked()
    {
        LookForData();
        if (movingWorld || m_OWEnvData == null)
            return;
        StartCoroutine(MoveWorld());
    }
    public IEnumerator MoveWorld()
    {
        movingWorld = true;
        Quaternion startRotation = m_OWEnvData.m_WorldGround.rotation;
        Quaternion endRotation = startRotation * Quaternion.Euler(m_OWEnvData.m_WorldRotation * m_OWEnvData.m_MoveAngle);
        float moveTime = m_OWEnvData.m_MoveTime;
        float currentTime = 0;
        while (true)
        {
            float evaluationTime = currentTime / moveTime;
            Quaternion currentRotation = Quaternion.Lerp(startRotation, endRotation, m_OWEnvData.m_SpeedCurve.Evaluate(evaluationTime));
            m_OWEnvData.m_WorldGround.rotation = currentRotation;
            CheckUpOnPropLines();
            if (currentTime >= moveTime)
                break;
            yield return null;
            currentTime += Time.deltaTime;
            currentTime = Mathf.Min(currentTime, moveTime);
        }
        movingWorld = false;
    }
    #endregion


    #region Props
    void CheckUpOnPropLines()
    {
        if (!m_OWEnvData.HasPropData)
            return;
        float angle;
        float sideOffset = m_OWEnvData.m_PropSideOffset * m_OWEnvData.m_PropWidth;
        float angleOffset = m_OWEnvData.m_PropAngleOffset * m_OWEnvData.m_PropLineAngle;
        foreach (OverworldPropLineHandler item in _propLines)
        {
            angle = AngleToWorld(item.transform.up);
            if (angle <= m_OWEnvData.m_AngleLimits.y)
            {
                angle += m_OWEnvData.m_AngleLimits.x - m_OWEnvData.m_AngleLimits.y;
                ClearPropLine(item);
                PreparePropLine(item, angle, sideOffset, angleOffset);
            }
        }
    }
    float AngleToWorld(Vector3 direction)
    {
        return Vector3.SignedAngle(m_OWEnvData.m_WorldUp, direction, m_OWEnvData.m_WorldRotation);
    }
    void ClearPropLine(OverworldPropLineHandler lineHandler)
    {
        lineHandler.transform.position = m_OWEnvData.m_GroundCenterPosition;
        lineHandler.transform.rotation = Quaternion.identity;
        foreach (SpriteRenderer item in lineHandler.InUseRenderers)
        {
            item.gameObject.SetActive(false);
            item.transform.localRotation = Quaternion.identity;
            _idleProps.Add(item);
        }
    }
    void PreparePropLine(OverworldPropLineHandler lineHandler, float angle, float sideOffset, float angleOffset)
    {
        Transform lineTransform = lineHandler.transform;
        List<SpriteRenderer> propLineList = new List<SpriteRenderer>();
        lineHandler.InUseRenderers = propLineList;

        //Fill list
        float randomNum;
        SpriteRenderer rend;
        float xPosition;
        float extraAngle;
        Transform propTransform;
        Vector3 upDirection;

        for (int j = 0; j < m_OWEnvData.m_PropSideCount; j++)
        {
            //Left side
            randomNum = Random.Range(0f, 1f);
            if (randomNum <= m_OWEnvData.m_PropSpawnChance)
            {
                rend = GetIdleProp(lineTransform);
                propTransform = rend.transform;
                rend.sprite = m_OWEnvData.m_SelectableProps[Random.Range(0, m_OWEnvData.m_SelectableProps.Length)];
                if (m_OWEnvData.m_UseCustomFlip)
                    rend.flipX = m_OWEnvData.m_LeftCustomFlipX;
                else
                    rend.flipX = Random.Range(0, 2) > 0;
                //Center x Pos
                xPosition = m_OWEnvData.m_InternalLimits.x - m_OWEnvData.m_PropWidth * j - m_OWEnvData.m_PropWidth * .5f;
                //random x offset
                xPosition += Random.Range(-sideOffset, sideOffset);

                //random angle offset
                extraAngle = Random.Range(-angleOffset, angleOffset);

                propTransform.position = m_OWEnvData.m_WorldRotation * xPosition + m_OWEnvData.m_WorldUp * m_OWEnvData.m_GroundVerticalOffset;

                propTransform.RotateAround(m_OWEnvData.m_PropLocator.position, m_OWEnvData.m_WorldRotation, extraAngle);
                upDirection = (propTransform.position - m_OWEnvData.m_PropLocator.position);
                upDirection.x = 0;
                propTransform.up = upDirection.normalized;
                rend.gameObject.SetActive(true);
                propLineList.Add(rend);
            }

            //Right side
            randomNum = Random.Range(0f, 1f);
            if (randomNum <= m_OWEnvData.m_PropSpawnChance)
            {
                rend = GetIdleProp(lineTransform);
                propTransform = rend.transform;
                rend.sprite = m_OWEnvData.m_SelectableProps[Random.Range(0, m_OWEnvData.m_SelectableProps.Length)];
                if (m_OWEnvData.m_UseCustomFlip)
                    rend.flipX = m_OWEnvData.m_RightCustomFlipX;
                else
                    rend.flipX = Random.Range(0, 2) > 0;
                //Center x Pos
                xPosition = m_OWEnvData.m_InternalLimits.y + m_OWEnvData.m_PropWidth * j + m_OWEnvData.m_PropWidth * .5f;
                //random x offset
                xPosition += Random.Range(-sideOffset, sideOffset);

                //random angle offset
                extraAngle = Random.Range(-angleOffset, angleOffset);

                propTransform.position = m_OWEnvData.m_WorldRotation * xPosition + m_OWEnvData.m_WorldUp * m_OWEnvData.m_GroundVerticalOffset;

                propTransform.RotateAround(m_OWEnvData.m_PropLocator.position, m_OWEnvData.m_WorldRotation, extraAngle);
                upDirection = (propTransform.position - m_OWEnvData.m_PropLocator.position);
                upDirection.x = 0;
                propTransform.up = upDirection.normalized;
                rend.gameObject.SetActive(true);
                propLineList.Add(rend);
            }
        }

        lineTransform.RotateAround(m_OWEnvData.m_PropLocator.position, m_OWEnvData.m_WorldRotation, angle);
        lineTransform.up = (lineTransform.position - m_OWEnvData.m_PropLocator.position).normalized;
    }
    OverworldPropLineHandler GenerateLinePropHandler()
    {
        OverworldPropLineHandler line = Instantiate(m_OWEnvData.m_PropLineTemplate);
        line.transform.SetParent(m_OWEnvData.m_PropLocator);
        line.transform.position = m_OWEnvData.m_GroundCenterPosition;
        line.gameObject.SetActive(true);
        return line;
    }
    SpriteRenderer GetIdleProp(Transform propLine)
    {
        SpriteRenderer rend;
        if (_idleProps.Count > 0)
        {
            rend = _idleProps[0];
            _idleProps.RemoveAt(0);
        }
        else
        {
            rend = Instantiate(m_OWEnvData.m_PropTemplate);
        }

        rend.transform.SetParent(propLine);

        return rend;
    }
    #endregion
}
