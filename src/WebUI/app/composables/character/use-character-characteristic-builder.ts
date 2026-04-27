import type { MaybeRefOrGetter } from 'vue'

import { computed, ref, toValue } from 'vue'

import type {
  CharacterCharacteristics,
  CharacteristicKey,
  CharacteristicSectionKey,
  CharacterPerkType,
} from '~/models/character'
import type { CharacteristicState } from '~/services/character-service'

import {
  allCharacteristicRequirementSatisfied,
  ATTRIBUTES_TO_SKILLS_RATE,
  characteristicRequirementSatisfied,
  computeHealthPoints,
  createDefaultCharacteristic,
  createEmptyCharacteristic,
  getCharacteristicCost,
  SKILLS_TO_ATTRIBUTES_RATE,
  wppForAgility,
  wppForWeaponMaster,
} from '~/services/character-service'
import { mergeObjectWithSum, objectEntries } from '~/utils/object'

export const useCharacterCharacteristicBuilder = (
  characteristicsInitial: MaybeRefOrGetter<CharacterCharacteristics>,
) => {
  const delta = ref<CharacterCharacteristics>(createEmptyCharacteristic())
  const deltaPerks = ref<CharacterPerkType[]>([])

  function reset() {
    delta.value = createEmptyCharacteristic()
    deltaPerks.value = []
  }

  const isDirty = computed(() =>
    ['attributes', 'skills', 'weaponProficiencies'].some(
      key => delta.value[key as keyof typeof delta.value].points !== 0,
    ) || deltaPerks.value.length > 0
  )

  const characteristics = computed<CharacterCharacteristics>(() => {
    const initial = toValue(characteristicsInitial)
    const base = { ...delta.value }

    // Compute final selectedPerks by applying toggles (deltaPerks) to initial
    const finalSelectedPerks = [...initial.perks.selectedPerks]
    for (const perkType of deltaPerks.value) {
      const idx = finalSelectedPerks.indexOf(perkType)
      if (idx >= 0) {
        finalSelectedPerks.splice(idx, 1)
      } else {
        finalSelectedPerks.push(perkType)
      }
    }

    const merged = objectEntries(initial).reduce(
      (obj, [key, values]) => {
        if (key === 'perks') {
          return {
            ...obj,
            [key]: {
              points: initial.perks.points - deltaPerks.value.length,
              selectedPerks: finalSelectedPerks,
            },
          }
        }
        return {
          ...obj,
          [key]: mergeObjectWithSum(
            obj[key],
            values as unknown as Record<string, number>,
          ),
        }
      },
      { ...base },
    )

    return merged
  })

  const isChangeValid = computed(() =>
    ['attributes', 'skills', 'weaponProficiencies'].every(
      key => characteristics.value[key as keyof CharacterCharacteristics].points >= 0,
    )
    && allCharacteristicRequirementSatisfied(characteristics.value),
  )

  const canConvertAttributesToSkills = computed(() => characteristics.value.attributes.points >= ATTRIBUTES_TO_SKILLS_RATE)

  const canConvertSkillsToAttributes = computed(() => characteristics.value.skills.points >= SKILLS_TO_ATTRIBUTES_RATE)

  const defaults = createDefaultCharacteristic()

  const getCharacteristicState = (
    section: CharacteristicSectionKey,
    key: CharacteristicKey,
    noMinLimit = false,
  ): CharacteristicState => {
    const initialValue = noMinLimit
      ? (defaults[section] as any)[key] as number
      : (toValue(characteristicsInitial)[section] as any)[key] as number

    const value = (characteristics.value[section] as any)[key] as number

    const costToIncrease = getCharacteristicCost(section, key, value + 1) - getCharacteristicCost(section, key, value)

    const requirement = characteristicRequirementSatisfied(section, key, value, characteristics.value)
    const nextRequirement = characteristicRequirementSatisfied(section, key, value + 1, characteristics.value)

    return {
      value,
      min: initialValue,
      max: value + ((costToIncrease <= characteristics.value[section].points && (nextRequirement !== null ? nextRequirement.satisfied : true)) ? 1 : 0),
      requirement,
      costToIncrease,
    }
  }

  const onTogglePerk = (perkType: CharacterPerkType): void => {
    const idx = deltaPerks.value.indexOf(perkType)
    if (idx >= 0) {
      deltaPerks.value.splice(idx, 1)
    } else {
      deltaPerks.value.push(perkType)
    }
  }

  const onInput = (
    section: CharacteristicSectionKey,
    key: CharacteristicKey,
    newValue: number,
  ): void => {
    const deltaSection = delta.value[section]
    const oldValue = characteristics.value[section][key as keyof typeof deltaSection]

    const oldCost = getCharacteristicCost(section, key, oldValue)
    const newCost = getCharacteristicCost(section, key, newValue)
    const costToIncrease = newCost - oldCost

    if (costToIncrease > characteristics.value[section].points) {
      return
    }

    const requirement = characteristicRequirementSatisfied(section, key, newValue, characteristics.value)

    if (requirement !== null && requirement.satisfied === false) {
      return
    }

    deltaSection.points += (oldCost - newCost)
    deltaSection[key as keyof typeof deltaSection] = newValue - toValue(characteristicsInitial)[section][key as keyof typeof deltaSection]

    if (key === 'agility') {
      delta.value.weaponProficiencies.points += wppForAgility(newValue) - wppForAgility(oldValue)
    }
    else if (key === 'weaponMaster') {
      delta.value.weaponProficiencies.points += wppForWeaponMaster(newValue) - wppForWeaponMaster(oldValue)
    }
  }

  const onInputWithAutoClamp = (section: CharacteristicSectionKey, key: CharacteristicKey, targetValue: number): void => {
    const initialValue = (toValue(characteristicsInitial)[section] as any)[key]
    let valueToTry = Math.min(targetValue, 250) // avoid long loop

    while (valueToTry >= initialValue) {
      onInput(section, key, valueToTry)
      const afterValue = (characteristics.value[section] as any)[key]

      if (afterValue === valueToTry) {
        return
      }

      valueToTry--
    }
  }

  const onResetField = (section: CharacteristicSectionKey, key: CharacteristicKey): void => {
    const { value, min } = getCharacteristicState(section, key)
    if (value > min) {
      onInput(section, key, min)
    }
  }

  const onFillField = (section: CharacteristicSectionKey, key: CharacteristicKey): void => {
    let { value, max } = getCharacteristicState(section, key)
    while (max > value) {
      onInput(section, key, max)
      const { value: newValue, max: newMax } = getCharacteristicState(section, key)
      max = newMax
      value = newValue
    }
  }

  const healthPoints = computed(() => computeHealthPoints(
    characteristics.value.skills.ironFlesh,
    characteristics.value.attributes.strength,
  ))

  return {
    canConvertAttributesToSkills,
    canConvertSkillsToAttributes,
    characteristics,
    getCharacteristicState,
    isChangeValid,
    onInput,
    onInputWithAutoClamp,
    onFillField,
    onResetField,
    onTogglePerk,
    reset,
    isDirty,
    healthPoints,
  }
}
