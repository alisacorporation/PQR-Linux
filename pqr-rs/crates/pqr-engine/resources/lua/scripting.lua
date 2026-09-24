
        function PQR_IsCastingSpell(spellID)
            local spellName = GetSpellInfo(spellID)
            local spellCasting = UnitCastingInfo("player")
                if spellCasting == nil then
                    spellCasting = UnitChannelInfo("player")
                end
                if spellCasting == spellName then
                    return true
                else
                    return false
                end
        end

        function PQR_NotBehindTarget()
            if PQR_BehindTime + 3 > GetTime() then
                return true
            else
                return false
            end
        end

        function PQR_IsMoving(seconds) 
            local PQR_MoltenFeathers = UnitBuffID("player", 98767)
            if PQR_CurrentMovingTime >= seconds and PQR_MoltenFeathers == nil then
                return true
            else
                return false
            end
        end

        function PQR_IsOutOfSight(unit, seconds)
            local secondsCheck = seconds
            if secondsCheck == nil then
                secondsCheck = 3
            end
            local unitCheck = unit
            if unitCheck == nil then
                unitCheck = "target"
            end
            local PQR_TargetName = UnitName(unitCheck)

            if PQR_TargetName ~= nil then
                for i=1,10000 do
                    if PQR.losTable.name[i] == nil then
                        return false
                    end
                    if PQR.losTable.name[i] == PQR_TargetName then
                        if PQR.losTable.time[i] > GetTime() - secondsCheck then
                            return true
                        else
                            return false
                        end
                    end
                end
            end
            return false
        end

        function UnitBuffID(unit, spellID, filter)
	        local spellName = GetSpellInfo(spellID)
	        if filter == nil then
		        return UnitBuff(unit, spellName)
	        else
		        return UnitBuff(unit, spellName, nil, filter)
	        end
        end

        function UnitDebuffID(unit, spellID, filter)
	        local spellName = GetSpellInfo(spellID)
	        if filter == nil then
		        return UnitDebuff(unit, spellName)
	        else
		        return UnitDebuff(unit, spellName, nil, filter)
	        end
        end
