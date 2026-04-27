<script setup lang="ts">
import type { CharacterPerks, CharacterPerkType } from '~/models/character'

interface PerkEntry {
  key: CharacterPerkType
}

defineProps<{
  perks: CharacterPerks
  readonly?: boolean
}>()

const emit = defineEmits<{
  toggle: [perkType: CharacterPerkType]
}>()

const availablePerks: PerkEntry[] = [
  { key: 'Headhunter' },
  { key: 'Berserker' },
  { key: 'Marksman' },
  { key: 'Tank' },
  { key: 'FleetFooted' },
  { key: 'ShieldExpert' },
  { key: 'HorseArcher' },
  { key: 'Brusier' },
  { key: 'QuickHands' },
  { key: 'Strong' },
  { key: 'Veteran' },
  { key: 'Hardheaded' },
  { key: 'Deflector' },
  { key: 'Armorer' },
  { key: 'ArmorPiercer' },
  { key: 'Executioner' },
  { key: 'BeastSlayer' },
  { key: 'ShieldBreaker' },
  { key: 'Fate' },
  { key: 'QuiverMaster' },
  { key: 'Regeneration' },
  { key: 'BountyHunter' },
]


</script>

<template>
  <UCard
    :ui="{ body: 'p-0! overflow-hidden', header: 'px-4! py-3' }"
  >
    <template #header>
      <UiDataCell>
        <UiTextView variant="p">
          {{ $t(`builder.perks.title`) }} -
          <span
            class="font-bold"
            :class="[perks.points < 0 ? 'text-error' : 'text-success']"
          >
            {{ perks.points }}
          </span>
        </UiTextView>
      </UiDataCell>
    </template>

    <UiDataCell
      v-for="perk in availablePerks"
      :key="perk.key"
      class="w-full px-4 py-2.5 hover:bg-muted"
      :class="{
        'bg-primary/10': perks.selectedPerks.includes(perk.key),
      }"
    >
      <div class="flex items-center gap-2">
        <UCheckbox
          :model-value="perks.selectedPerks.includes(perk.key)"
          :disabled="readonly || (perks.points <= 0 && !perks.selectedPerks.includes(perk.key))"
          @update:model-value="emit('toggle', perk.key)"
        />
        <UiTextView variant="caption">
          {{ $t(`builder.perks.children.${perk.key}.title`) }}
        </UiTextView>
      </div>

      <template #rightContent>
        <UiTextView
          variant="caption"
          class="text-right"
        >
          {{ $t(`builder.perks.children.${perk.key}.desc`) }}
        </UiTextView>
      </template>
    </UiDataCell>
  </UCard>
</template>
