
        
        function PQR_Debug(strText)
            if PQR_DebugEnabled == true then
                if PQR_DebugLog == nil then PQR_DebugLog = "" end
                PQR_DebugLog = PQR_DebugLog .. strText .. "\n"
                print("|cffff7d0a<|r|cffffd200PQR|r|cffff7d0a>|r |cffffff00[DEBUG]|r "..strText);
            end
        end
        function PQR_FlushDebugLog()
            local s = PQR_DebugLog
            PQR_DebugLog = ""
            if s == nil or s == "" then return "" end
            s = string.gsub(s, "\n", "\\n")
            if #s > 1800 then
                local out = string.sub(s, 1, 1800)
                local rest = string.sub(s, 1801)
                rest = string.gsub(rest, "\\n", "\n")
                PQR_DebugLog = rest
                return out
            end
            return s
        end

        function PQR_SetDebug(enabled)
            if enabled == true then
                PQR_DebugEnabled = true
            else
                PQR_DebugEnabled = false
            end
        end

        function PQR_WriteToChat(strText)
            if PQR_ShowChat == true then
                print("|cffff7d0a<|r|cffffd200PQR|r|cffff7d0a>|r "..strText)
            end
        end

        function PQR_EventFrame_OnUpdate(self, elapsed)
	        PQR_totalElapsed = PQR_totalElapsed + elapsed;
	        PQR_totalElapsed2 = PQR_totalElapsed2 + elapsed;
            while (PQR_totalElapsed2 > PQR_UpdateInterval2) do
                PQR_RegisterMovement(PQR_totalElapsed2)
                PQR_SetRecastDelay()
                PQR_ExecuteInterrupt()
                PQR_totalElapsed2 = PQR_totalElapsed2 - PQR_UpdateInterval2
            end
            if PQR_ChangeIntervalFlag == 1 then
                PQR_UpdateInterval = PQR_NewInterval
                PQR_ChangeIntervalFlag = 0
                PQR_totalElapsed = 0
            else
                while (PQR_totalElapsed > PQR_UpdateInterval) do
                    PQR_RegisterMovement(PQR_totalElapsed)
                    PQR_SetRecastDelay()
                    PQR_ExecuteBot()
                    PQR_totalElapsed = PQR_totalElapsed - PQR_UpdateInterval
                end
            end
        end

        function PQR_EventFrame_OnEvent(self,event,...)
            if event == "PLAYER_ENTERING_WORLD" then
                PQR_EventFrame:Show()
                SetMapToCurrentZone()
                PQR_Debug("Setting Map to Current Zone.")
            elseif event =="PLAYER_REGEN_DISABLED" then
                SetMapToCurrentZone()
                PQR_Debug("Setting Map to Current Zone.")
            elseif event == "PLAYER_LOGOUT" then
                PQR_EnableInterrupt(false)
                PQR_EnableBot("")
            elseif event == "UNIT_SPELLCAST_FAILED" then
                local source, spellName = ...
                PQR_DropThreat("FAILED", source, spellName) 
            elseif event == "UNIT_SPELLCAST_FAILED_QUIET" then
                local source, spellName = ...
                PQR_DropThreat("FAILED", source, spellName) 
            elseif event == "UI_ERROR_MESSAGE" then
                local errorMessage = ...
                if errorMessage == SPELL_FAILED_NOT_BEHIND then
                    PQR_BehindTime = GetTime()
                elseif errorMessage == SPELL_FAILED_LINE_OF_SIGHT then
                    if PQR_LastTarget ~= nil then
                        if PQR_LastTarget ~= "none" and PQR_LastTime > GetTime() - 1 then
                            --LOS message in the last 1 second. Register it.
                            for i=1,10000 do
                                if PQR.losTable.name[i] == nil or PQR.losTable.name[i] == PQR_LastTarget then
                                    --we got to an empty slot in the table
                                    PQR.losTable.name[i] = PQR_LastTarget
                                    PQR.losTable.time[i] = PQR_LastTime
                                    PQR_Debug("Added LOS for unit "..PQR_LastTarget.." at index "..i)
                                    break
                                end
                            end
                            PQR_LastTarget = "none"
                            PQR_LastTime = 0
                        end
                    end
                end
            elseif event == "UNIT_SPELLCAST_SUCCEEDED" then
                local unitCaster, unitSpell, _, _, unitSpellID = ...
                if unitCaster == "player" then
                    PQR_SetRecastDelay(unitSpell)
                end
                if unitCaster ~= "player" then
                    return
                end
                if unitSpellID ~= 75 then --Auto Shot
                    if unitSpellID == 56641 or unitSpellID == 77767 then --Steady or Cobra
                        PQR_SteadyCount = PQR_SteadyCount + 1
                        PQR_LastCasted = unitSpellID
                    else
                        PQR_SteadyCount = 0
                        PQR_LastCasted = unitSpellID
                    end
                end
            elseif event == "UNIT_SPELLCAST_SENT" then
                local unitCaster, unitSpell = ...
                local vanishName = GetSpellInfo(1856)
                local mdName = GetSpellInfo(34477)
                local feignName = GetSpellInfo(5384)
                if unitSpell == vanishName then
                    PQR_DropDelay = GetTime() + 0.5
                end
                if unitSpell == feignName then
                    PQR_DropDelay = GetTime() + 1
                end
                if unitSpell == mdName then
                    PQR_DropDelay = GetTime() + 2.0
                end
            end
        end
        
        function PQR_ExecuteBot()
            --this is the main bot execution function. 
            if UnitAffectingCombat ~= nil then
                --make sure we arent at a loading screen, DCd, etc.
 	            PQR_Combat = UnitAffectingCombat("player")
                if PQR_Combat == nil then 
                    PQR_Combat = UnitAffectingCombat("pet")
                end
                if PQR_RequireCombat == false and PQR_RotationDesignation == false then
                    PQR_Combat = 1
                end
                if PQR ~= nil then
                    if PQR_RotationDesignation == true and PQR[0].priorityTable.requireCombat == false then
                        PQR_Combat = 1
                    end
                end
                local PQR_ExecuteNext = GetCVar("violenceLevel")
                if tonumber(PQR_ExecuteNext) ~= nil then
                    PQR_ExecuteNext = PQR_ExecuteNext - 5
                    SetCVar("violenceLevel", 2)
                else
                    PQR_ExecuteNext = 0
                end

                if PQR_ExecuteNext > 0 then
                    --cast the next ability in X queue.
                    PQR_Interrupt()
                    PQR_CastNext(PQR_ExecuteNext)
                else
                    if PQR_ManualMode == false and PQR_BotEnabled == true and PQR_Combat ~= nil then
                        --auto rotation mode is running... execute next.
                        PQR_CastNext(0)
                    end
                end
            end
        end

        function PQR_CastNext(rotation)
            if PQR[rotation] == nil then
                return
            end

            local nextAbility = PQR_NextAbility(rotation)
            if nextAbility ~= -1 then
                local abName = PQR[rotation].priorityTable.name[nextAbility]
                if abName == nil or abName == "" then abName = "#"..nextAbility end
                local isSection = string.sub(abName, 1, 2) == "--"
                if not isSection then
                    PQR_Debug("Selected "..abName.." spid="..PQR[rotation].priorityTable.spid[nextAbility])
                end

                --Run luaBefore
                PQR[rotation].priorityTable.luaBefore[nextAbility]()

                --split actions and send RunMacroText
                    if PQR[rotation].priorityTable.actn[nextAbility] ~= "" and PQR[rotation].priorityTable.actn[nextAbility] ~= nil then
                        local tbl = { strsplit("|", PQR[rotation].priorityTable.actn[nextAbility]) }
                        for i=1,100 do
                            if tbl[i] == nil then
                                break
                            end
                            if tbl[i] ~= "" then
                                PQR_Debug("Running Action: "..tbl[i])
                                RunMacroText(tbl[i])
                            end
                        end
                    end
                --send spell ID
                    local castingID = PQR[rotation].priorityTable.spid[nextAbility]
                    if castingID ~= 0 then
                        local castingName = GetSpellInfo(PQR[rotation].priorityTable.spid[nextAbility])
                        PQR_LastTime = GetTime()
                        if PQR[rotation].priorityTable.targetCast[nextAbility] == "player" then
                            PQR_Debug("Casting Spell "..castingName.." ("..castingID..") on PLAYER ["..abName.."]")
                            PQR_LastTarget = UnitName(PQR[rotation].priorityTable.targetCast[nextAbility])
                            CastSpellByID(castingID, "player")
                        elseif PQR[rotation].priorityTable.targetCast[nextAbility] == "focus" then
                            PQR_Debug("Casting Spell "..castingName.." ("..castingID..") on FOCUS ["..abName.."]")
                            PQR_LastTarget = UnitName(PQR[rotation].priorityTable.targetCast[nextAbility])
                            CastSpellByID(castingID, "focus")
                        elseif PQR[rotation].priorityTable.targetCast[nextAbility] == "pet" then
                            PQR_Debug("Casting Spell "..castingName.." ("..castingID..") on PET ["..abName.."]")
                            PQR_LastTarget = UnitName(PQR[rotation].priorityTable.targetCast[nextAbility])
                            CastSpellByID(castingID, "pet")
                        elseif PQR[rotation].priorityTable.targetCast[nextAbility] == "mouseover" then
                            PQR_Debug("Casting Spell "..castingName.." ("..castingID..") on MOUSEOVER ["..abName.."]")
                            PQR_LastTarget = UnitName(PQR[rotation].priorityTable.targetCast[nextAbility])
                            CastSpellByID(castingID, "mouseover")
                        elseif PQR[rotation].priorityTable.targetCast[nextAbility] == "click" then
                            PQR_Debug("Casting Spell "..castingName.." ("..castingID..") on MOUSE LOCATION ["..abName.."]")
                            PQR_LastTarget = "none"
                            CastSpellByID(castingID)
                            local PQR_TargetName = UnitName("target")
                            CameraOrSelectOrMoveStart()
                            CameraOrSelectOrMoveStop()
                            local PQR_TargetNameNew = UnitName("target")
                            if PQR_TargetName ~= nil then
                                if PQR_TargetName ~= PQR_TargetNameNew then
                                    TargetLastTarget()
                                end
                            end
                        elseif PQR[rotation].priorityTable.targetCast[nextAbility] == "custom" then
                            PQR_Debug("Casting Spell "..castingName.." ("..castingID..") on CUSTOM TARGET "..PQR_CustomTarget.." ["..abName.."]")
                            if UnitExists(PQR_CustomTarget) then
                                PQR_LastTarget = UnitName(PQR_CustomTarget)
                                CastSpellByID(castingID, PQR_CustomTarget)
                            else
                                PQR_Debug("Custom Target does not exist!")
                            end
                            PQR_CustomTarget = ""
                        else
                            PQR_Debug("Casting Spell "..castingName.." ("..castingID..") on TARGET ["..abName.."]")
                            PQR_LastTarget = UnitName(PQR[rotation].priorityTable.targetCast[nextAbility])
                            CastSpellByID(castingID, "target")
                        end
                    else
                        if not isSection then
                            PQR_Debug("Lua Ability "..abName.." (SpellID 0)")
                        end
                    end

                --Run luaAfter
                PQR[rotation].priorityTable.luaAfter[nextAbility]()

            end            
        end

        function PQR_NextAbility(rotation)    
	        if PQR[rotation] ~= nil then
		        local _, _, _, _, _, playerChanneling = UnitChannelInfo("player")
		        if playerChanneling ~= nil then
			        local currentTime = GetTime() * 1000
			        if (playerChanneling - currentTime) > 100 then
                        PQR_IsChanneling = true
                    else
                        PQR_IsChanneling = false
			        end
                else
                    PQR_IsChanneling = false
		        end

		        for i=0,1023 do
			        if PQR[rotation].priorityTable.index[i] ~= nil then
                        if PQR_DropDelay <= GetTime() then
				            if PQR[rotation].priorityTable.spid[i] ~= 0 then
					            if PQR_SpellAvailable(PQR[rotation].priorityTable.spid[i]) == true and PQR[rotation].priorityTable.delay[i] <= (GetTime() * 1000) then
						            local PQR_Result = PQR[rotation].priorityTable.test[i]()
						            if PQR_Result == true then
                                        if PQR_IsChanneling == true then
                                            if PQR[rotation].priorityTable.cancelChannel[i] == true then
							                    return i
                                            end
                                        else
                                            return i
                                        end
						            end
					            end
				            else
					            local PQR_Result = PQR[rotation].priorityTable.test[i]()
						            if PQR_Result == true then
                                        if PQR_IsChanneling == true then
                                            if PQR[rotation].priorityTable.cancelChannel[i] == true then
							                    return i
                                            end
                                        else
                                            return i
                                        end
						            end
				            end
                        end
			        end
		        end
	        end
	        return -1
        end

        function PQR_ExecuteInterrupt()
            if UnitAffectingCombat ~= nil then
                PQR_Interrupt()
            end
        end

        function PQR_Interrupt()
            if PQR_InterruptEnabled == false or PQR_InterruptEnabled == nil then
                return
            end

            if PQR == nil then
                return
            end

            if PQR.interruptTable == nil then
                return
            end
            
		    if PQR_InterruptTest() == true then
                PQR_Debug("Casting Interrupt...")
			    CastSpellByID(PQR_MyInterruptSpell())
		    end
        end

        function PQR_InterruptTest()
	        local pq_castingName, _, _, _, pq_castingStartTime, _, _, _, pq_castingInterrupt = UnitCastingInfo("target")
	        if pq_castingInterrupt ~= nil then
		        if pq_castingInterrupt == true then
			        pq_castingInterrupt = false
		        else
			        pq_castingInterrupt = true
		        end 
	        end

	        local pq_channelName, _, _, _, pq_channelStartTime, _, _, pq_channelInterrupt = UnitChannelInfo("target")
	        if pq_channelInterrupt ~= nil then
		        pq_castingName = pq_channelName
                pq_castingStartTime = pq_channelStartTime
		        if pq_channelInterrupt == true then
			        pq_castingInterrupt = false
		        else
			        pq_castingInterrupt = true
		        end
	        end

            

	        if pq_castingName == nil then
		        pq_castingName = ""
	        end 

	        if pq_castingInterrupt == nil then
		        pq_castingInterrupt = false
            else
                local PQR_msCastingSpell = GetTime() * 1000 - pq_castingStartTime
                if PQR_msCastingSpell < PQR_InterruptDelay then
                    pq_castingInterrupt = false
                end
	        end

            if UnitCanAttack("player", "target") == nil then
                --we cannot attack/interrupt the target.
                pq_castingInterrupt = false
            end
          
	        local PQR_CastInterrupt = false
	        if pq_castingInterrupt == true then
		        local _, InterruptCD, _ = GetSpellCooldown(PQR_MyInterruptSpell())
		        if InterruptCD == 0 then
			        if PQR_InterruptAllSpells == true then
				        PQR_CastInterrupt = true
			        end
			
			        for i=0,1023 do
				        --check to see if any abilities need to be cast.
				        if PQR.interruptTable.spell[i] ~= nil then
					        if strupper(PQR.interruptTable.spell[i]) == strupper(pq_castingName) then
						        PQR_CastInterrupt = true
                                break
					        end
				        else
					        break
				        end
			        end
		        end
	        end
	
	        if PQR_CastInterrupt == true then
		        return true
	        else
		        return false
	        end
        end

        function PQR_MyInterruptSpell()
	        if UnitClass ~= nil then
		        local _, PQR_UnitClass = UnitClass("player")
		
		        if PQR_UnitClass == "DEATHKNIGHT" then
			        return 47528
		        elseif PQR_UnitClass == "DRUID" then
			        local catForm = UnitBuffID("player", 768)
			        if catForm ~= nil then
				        return 80965
			        else
				        return 80964
			        end
		        elseif PQR_UnitClass == "HUNTER" then
			        return 34490
		        elseif PQR_UnitClass == "MAGE" then
			        return 2139
		        elseif PQR_UnitClass == "PALADIN" then
			        return 96231
		        elseif PQR_UnitClass == "PRIEST" then
			        return 15487
		        elseif PQR_UnitClass == "ROGUE" then
			        return 1766
		        elseif PQR_UnitClass == "SHAMAN" then
			        return 57994
		        elseif PQR_UnitClass == "WARLOCK" then
			        return 19647
		        elseif PQR_UnitClass == "WARRIOR" then
			        return 6552
		        else
			        return 0
		        end
	        else
		        return 0
	        end
        end

        function PQR_SetRecastDelay(manualSpellName)
           
           local PQR_CurrentSpellName = UnitCastingInfo("player")
           if PQR_CurrentSpellName == nil then
                PQR_CurrentSpellName = UnitCastingInfo("player")
            end 
            if manualSpellName ~= nil then
                PQR_CurrentSpellName = manualSpellName
            end
            if PQR_CurrentSpellName == nil then
                return
            end
            PQR_Debug("SetRecastDelay called for spell: "..PQR_CurrentSpellName)

            if PQR_ManualMode == false and PQR_BotEnabled == true then
                --PQR[0]
                for i=0,1023 do
                    if PQR[0].priorityTable.index[i] == nil then
                        break
                    else
                        if PQR[0].priorityTable.spid[i] ~= 0 then
                            local PQR_LookupSpell = GetSpellInfo(PQR[0].priorityTable.spid[i])
                            if PQR_LookupSpell == PQR_CurrentSpellName then
                                PQR[0].priorityTable.delay[i] = (GetTime() * 1000) + PQR[0].priorityTable.recast[i]
                            end
                        end
                    end
                end
            else
                --PQR[1]
                for i=0,1023 do
                    if PQR[1].priorityTable.index[i] == nil then
                        break
                    else
                        if PQR[1].priorityTable.spid[i] ~= 1 then
                            local PQR_LookupSpell = GetSpellInfo(PQR[1].priorityTable.spid[i])
                            if PQR_LookupSpell == PQR_CurrentSpellName then
                                PQR[1].priorityTable.delay[i] = (GetTime() * 1000) + PQR[1].priorityTable.recast[i]
                            end
                        end
                    end
                end

                --PQR[2]
                for i=0,1023 do
                    if PQR[2].priorityTable.index[i] == nil then
                        break
                    else
                        if PQR[2].priorityTable.spid[i] ~= 2 then
                            local PQR_LookupSpell = GetSpellInfo(PQR[2].priorityTable.spid[i])
                            if PQR_LookupSpell == PQR_CurrentSpellName then
                                PQR[2].priorityTable.delay[i] = (GetTime() * 1000) + PQR[2].priorityTable.recast[i]
                            end
                        end
                    end
                end

                --PQR[3]
                for i=0,1023 do
                    if PQR[3].priorityTable.index[i] == nil then
                        break
                    else
                        if PQR[3].priorityTable.spid[i] ~= 3 then
                            local PQR_LookupSpell = GetSpellInfo(PQR[3].priorityTable.spid[i])
                            if PQR_LookupSpell == PQR_CurrentSpellName then
                                PQR[3].priorityTable.delay[i] = (GetTime() * 1000) + PQR[3].priorityTable.recast[i]
                            end
                        end
                    end
                end

                --PQR[4]
                for i=0,1023 do
                    if PQR[4].priorityTable.index[i] == nil then
                        break
                    else
                        if PQR[4].priorityTable.spid[i] ~= 4 then
                            local PQR_LookupSpell = GetSpellInfo(PQR[4].priorityTable.spid[i])
                            if PQR_LookupSpell == PQR_CurrentSpellName then
                                PQR[4].priorityTable.delay[i] = (GetTime() * 1000) + PQR[4].priorityTable.recast[i]
                            end
                        end
                    end
                end
            end
        end

        if PQR_EventFrame == nil then
            PQR = {}
            PQR.interruptTable = {}
            PQR.interruptTable.spell = {}
            for i=0,4 do
                PQR[i] = {}
            end
            PQR_RotationDesignation = true
            PQR_ManualMode = false
            PQR_totalElapsed = 0.0
            PQR_totalElapsed2 = 0.0
            PQR_SteadyCount = 0
            PQR_LastCasted = 0
            PQR_DropDelay = 0
            PQR_ChangeIntervalFlag = 0
            PQR_NewInterval = 0.1
            PQR_UpdateInterval = 0.1
            PQR_UpdateInterval2 = 0.1
            PQR_BehindTime = 0
            PQR_CurrentX = 0
            PQR_CurrentY = 0
            PQR_StationaryX = 0
            PQR_StationaryY = 0
            PQR_StationaryTime = 0
            PQR_CurrentMovingTime = 0
            PQR_CurrentStationaryTime = 1
            PQR_ResetMovementTime = 1
            PQR_LastMovementTime = 0
            PQR_RequireCombat = true
            PQR_InterruptDelay = 0
            PQR_IsChanneling = false
            PQR_LastTarget = "none"
            PQR_LastTime = 0
            PQR_CustomTarget = ""
            PQR_EventFrame = CreateFrame("Frame")
            PQR_EventFrame:RegisterEvent("COMBAT_LOG_EVENT_UNFILTERED")
            PQR_EventFrame:RegisterEvent("PLAYER_ENTERING_WORLD")
            PQR_EventFrame:RegisterEvent("UNIT_SPELLCAST_SENT")
            PQR_EventFrame:RegisterEvent("PLAYER_LOGOUT")
            PQR_EventFrame:RegisterEvent("UNIT_SPELLCAST_FAILED")
            PQR_EventFrame:RegisterEvent("UNIT_SPELLCSAT_FAILED_QUIET")
            PQR_EventFrame:RegisterEvent("UI_ERROR_MESSAGE")
            PQR_EventFrame:RegisterEvent("UNIT_SPELLCAST_SUCCEEDED")
            PQR_EventFrame:RegisterEvent("PLAYER_REGEN_DISABLED")
            PQR_EventFrame:SetScript("OnUpdate", PQR_EventFrame_OnUpdate)
            PQR_EventFrame:SetScript("OnEvent", PQR_EventFrame_OnEvent)
            PQR_EventFrame:Show()
        end



        function PQR_RegisterMovement(elapsed)
            local posX, posY = GetPlayerMapPosition("player")
            local playerMovementSpeed = GetUnitSpeed("player")

            if playerMovementSpeed ~= 0 then
                PQR_CurrentMovingTime = PQR_CurrentMovingTime + elapsed
                PQR_CurrentStationaryTime = 0
            else
                if PQR_CurrentStationaryTime < PQR_ResetMovementTime then
                    PQR_CurrentStationaryTime = PQR_CurrentStationaryTime + elapsed
                end
                if PQR_CurrentStationaryTime > PQR_ResetMovementTime then
                    PQR_CurrentMovingTime = 0
                end
            end
         end

        function PQR_EnableInterrupt(disable)
            if PQR_InterruptEnabled == true then
                PlaySound(PQR_StopInterruptSound, "Master")
                PQR_WriteToChat("Interrupt Mode Disabled.")
                PQR_InterruptEnabled = false
            else
                if disable == nil then
                    PlaySound(PQR_StartInterruptSound, "Master")
                    PQR_WriteToChat("Interrupt Mode Enabled.")
                    PQR_InterruptEnabled = true
                else
                    if disable == true and PQR_InterruptEnabled == true then
                        PlaySound(PQR_StopInterruptSound, "Master")
                        PQR_WriteToChat("Interrupt Mode Disabled.")
                    end
                    PQR_InterruptEnabled = false
                end
            end
        end

        function PQR_EnableBot(mode)
            --This function will display the proper chat message regarding if the bot is switching modes or is enabling/disabling.
            PQR_ResetMovementTime = 1.0
            if (mode ~= PQR_BotRotation) then
                if mode == "" then
                    PlaySound(PQR_StopRotationSound, "Master")
                    PQR_WriteToChat("Rotation Mode Disabled.")
                    PQR_BotRotation = ""
                    PQR_BotEnabled = false
                else
                    if PQR_BotRotation == "" or PQR_BotRotation == nil then
                        PlaySound(PQR_StartRotationSound, "Master")
                    else
                        PlaySound(PQR_ChangeRotationSound, "Master")
                    end
                    local PQR_CorrectMode = strsplit("^&*", mode)
                    PQR_WriteToChat(PQR_CorrectMode.." Rotation Mode Enabled.")
                    SetMapToCurrentZone()
                    PQR_BotRotation = mode
                    PQR_BotEnabled = true
                end
            else
                if (mode == "* Manual") then
                    PQR_WriteToChat(mode.." Rotation Mode Enabled.")
                    PQR_BotRotation = mode
                    PQR_BotEnabled = true
                else
                    PlaySound(PQR_StopRotationSound, "Master")
                    PQR_WriteToChat("Rotation Mode Disabled.")
                    PQR_BotRotation = ""
                    PQR_BotEnabled = false
                end
            end
        end

        function PQR_ChangeInterval(newInterval)
            PQR_Debug("Changing Interval to: '"..newInterval.."'")
            if newInterval >= 20 and newInterval <= 1000 then
                PQR_NewInterval = (newInterval / 1000)
            else
                PQR_NewInterval = 0.1
            end
            PQR_ChangeIntervalFlag = 1
        end

        function PQR_DropThreat(successType, spellCaster, spellName)
            if spellCaster == "player" and PQR_isThreatDrop(spellName) == true then
		        if successType == "SUCCESS" then
			        PQR_DropDelay = 0
		        elseif successType == "FAILED" then
			        --player is attempting to drop threat but failing due to being on GCD. Delay any future updates for .5 seconds so player can drop threat.
			        
			        local start, duration, enabled = GetSpellCooldown(spellName);
			        local dropthreatCD = (start + duration - GetTime())
			
			        if dropthreatCD <= 5 then
                        PQR_DropDelay = GetTime() + 0.5
			        end
		        end
            end
        end

        function PQR_isThreatDrop(spellName)
            if PQTD == nil then
                PQTD = {}
            end
            PQTD[1] = GetSpellInfo(5384) --Feign Death
            PQTD[2] = GetSpellInfo(642) --Divine Shield
            PQTD[3] = GetSpellInfo(12042) --Arcane Power
            PQTD[4] = GetSpellInfo(66) --Invisibility
            for i=1,3 do
                if PQTD[i] == spellName then
                    return true
                end
            end
            return false
        end        

        function PQR_GCDSpell()
	        if UnitClass ~= nil then
		        local _, PQR_UnitClass = UnitClass("player")
                local _, build = GetBuildInfo()
                if tonumber(build) <= 2973 then
		            if PQR_UnitClass == "DEATHKNIGHT" then
			            return 52375
		            elseif PQR_UnitClass == "DRUID" then
			            return 774
		            elseif PQR_UnitClass == "HUNTER" then
			            return 56641
		            elseif PQR_UnitClass == "MAGE" then
			            return 1459
		            elseif PQR_UnitClass == "PALADIN" then
			            return 85256
		            elseif PQR_UnitClass == "PRIEST" then
			            return 2050
		            elseif PQR_UnitClass == "ROGUE" then
			            return 1752
		            elseif PQR_UnitClass == "SHAMAN" then
			            return 45284
		            elseif PQR_UnitClass == "WARLOCK" then
			            return 980
		            elseif PQR_UnitClass == "WARRIOR" then
			            return 1715
		            else
			            return 0
		            end
                else
		            if PQR_UnitClass == "DEATHKNIGHT" then
			            return 52375
		            elseif PQR_UnitClass == "DRUID" then
			            return 774
		            elseif PQR_UnitClass == "HUNTER" then
			            return 56641
		            elseif PQR_UnitClass == "MAGE" then
			            return 1459
		            elseif PQR_UnitClass == "PALADIN" then
			            return 85256
		            elseif PQR_UnitClass == "PRIEST" then
			            return 2050
		            elseif PQR_UnitClass == "ROGUE" then
			            return 1752
		            elseif PQR_UnitClass == "SHAMAN" then
			            return 45284
		            elseif PQR_UnitClass == "WARLOCK" then
			            return 980
		            elseif PQR_UnitClass == "WARRIOR" then
			            return 1715
		            else
			            return 0
		            end
                end
	        else
		        return 0
	        end
        end

        function PQR_SpellAvailable(SpellID)
	        local GCDSpell = PQR_GCDSpell()

	        if GetSpellCooldown ~= nil then
		        local GCDstartTime, GCDduration = GetSpellCooldown(GCDSpell)
		        local startTime, duration, enabled = GetSpellCooldown(SpellID)
		        local usableName, _, _, spellCost = GetSpellInfo(SpellID)
                local spellUsable = nil
                local _, build = GetBuildInfo()
                if tonumber(build) <= 12340 then
		            spellUsable = IsUsableSpell(usableName)
                else
                    spellUsable = IsUsableSpell(SpellID)
                end
		        pq_SpellAvailable = "false"
        
		        if startTime ~= nil and GCDstartTime ~= nil then
			        local timeLeft = startTime + duration - GetTime()
			        local GCDtimeLeft = GCDstartTime + GCDduration - GetTime()
				        if GCDtimeLeft <= 0 then
					        if timeLeft <= .25 then
						        if spellUsable ~= nil then
							        pq_SpellAvailable = "true"
						        end
					        end
				        else
					        if timeLeft <= GCDtimeLeft + 0.25 then
						        if spellUsable ~= nil then
							        pq_SpellAvailable = "true"
						        end
					        end
				        end
		        else
			        pq_SpellAvailable = "false"
		        end
	        else
		        pq_SpellAvailable = "false"
	        end
	
	        if pq_SpellAvailable == nil or pq_SpellAvailable == "false" then
		        return false
	        else
		        return true
	        end
        end

