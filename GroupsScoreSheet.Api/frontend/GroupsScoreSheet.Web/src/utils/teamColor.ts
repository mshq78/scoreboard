export type TeamColorVisual = {
  key: string
  label: string
  color: string
  border: string
  soft: string
  text: string
  hasColor: boolean
}

export const defaultTeamColorVisual: TeamColorVisual = {
  key: 'default',
  label: '',
  color: '',
  border: '',
  soft: '',
  text: '',
  hasColor: false,
}

const teamColorPalette = [
  {
    key: 'navyBlue',
    labels: ['آبی نفتی'],
    color: '#0f3f5f',
    border: 'rgba(15, 63, 95, 0.46)',
    soft: 'rgba(15, 63, 95, 0.08)',
    text: '#0f3f5f',
  },
  {
    key: 'blue',
    labels: ['آبی'],
    color: '#2563eb',
    border: 'rgba(37, 99, 235, 0.42)',
    soft: 'rgba(37, 99, 235, 0.08)',
    text: '#1d4ed8',
  },
  {
    key: 'green',
    labels: ['سبز'],
    color: '#16a34a',
    border: 'rgba(22, 163, 74, 0.42)',
    soft: 'rgba(22, 163, 74, 0.08)',
    text: '#15803d',
  },
  {
    key: 'red',
    labels: ['قرمز'],
    color: '#dc2626',
    border: 'rgba(220, 38, 38, 0.42)',
    soft: 'rgba(220, 38, 38, 0.08)',
    text: '#b91c1c',
  },
  {
    key: 'yellow',
    labels: ['زرد'],
    color: '#eab308',
    border: 'rgba(234, 179, 8, 0.52)',
    soft: 'rgba(234, 179, 8, 0.14)',
    text: '#854d0e',
  },
  {
    key: 'darkBlue',
    labels: ['سرمه ای', 'سرمه‌ای'],
    color: '#1e3a8a',
    border: 'rgba(30, 58, 138, 0.46)',
    soft: 'rgba(30, 58, 138, 0.08)',
    text: '#1e3a8a',
  },
  {
    key: 'black',
    labels: ['مشکی', 'سیاه'],
    color: '#111827',
    border: 'rgba(17, 24, 39, 0.45)',
    soft: 'rgba(17, 24, 39, 0.06)',
    text: '#111827',
  },
  {
    key: 'turquoise',
    labels: ['فیروزه ای', 'فیروزه‌ای'],
    color: '#0891b2',
    border: 'rgba(8, 145, 178, 0.42)',
    soft: 'rgba(8, 145, 178, 0.09)',
    text: '#0e7490',
  },
  {
    key: 'gray',
    labels: ['طوسی', 'خاکستری'],
    color: '#64748b',
    border: 'rgba(100, 116, 139, 0.45)',
    soft: 'rgba(100, 116, 139, 0.08)',
    text: '#475569',
  },
  {
    key: 'khaki',
    labels: ['خاکی'],
    color: '#a16207',
    border: 'rgba(161, 98, 7, 0.44)',
    soft: 'rgba(161, 98, 7, 0.10)',
    text: '#854d0e',
  },
  {
    key: 'cream',
    labels: ['کرم', 'کرمی'],
    color: '#d6b56d',
    border: 'rgba(214, 181, 109, 0.58)',
    soft: 'rgba(214, 181, 109, 0.16)',
    text: '#7c5c16',
  },
  {
    key: 'mustard',
    labels: ['خردلی'],
    color: '#ca8a04',
    border: 'rgba(202, 138, 4, 0.48)',
    soft: 'rgba(202, 138, 4, 0.13)',
    text: '#854d0e',
  },
  {
    key: 'orange',
    labels: ['نارنجی'],
    color: '#f97316',
    border: 'rgba(249, 115, 22, 0.45)',
    soft: 'rgba(249, 115, 22, 0.10)',
    text: '#c2410c',
  },
  {
    key: 'brown',
    labels: ['قهوه ای', 'قهوه‌ای'],
    color: '#92400e',
    border: 'rgba(146, 64, 14, 0.46)',
    soft: 'rgba(146, 64, 14, 0.09)',
    text: '#78350f',
  },
  {
    key: 'gold',
    labels: ['طلایی'],
    color: '#d97706',
    border: 'rgba(217, 119, 6, 0.50)',
    soft: 'rgba(217, 119, 6, 0.13)',
    text: '#92400e',
  },
] as const

type TeamColorMatch = TeamColorVisual & {
  normalizedLabel: string
}

function normalizePersianText(value: string): string {
  return value
    .replace(/[يى]/g, 'ی')
    .replace(/ك/g, 'ک')
    .replace(/[آأإٱ]/g, 'ا')
    .replace(/[\u064B-\u065F]/g, '')
    .replace(/ـ/g, '')
    .replace(/[\u200c\u200f\u200e]/g, ' ')
    .replace(/\s+/g, ' ')
    .trim()
}

function isWordCharacter(char: string | undefined): boolean {
  if (!char) {
    return false
  }

  return /[\p{L}\p{N}]/u.test(char)
}

function hasPhraseBoundary(text: string, startIndex: number, phraseLength: number): boolean {
  const before = text[startIndex - 1]
  const after = text[startIndex + phraseLength]

  return !isWordCharacter(before) && !isWordCharacter(after)
}

const normalizedTeamColorMatches: TeamColorMatch[] = teamColorPalette
  .flatMap((item) =>
    item.labels.map((label) => ({
      key: item.key,
      label,
      color: item.color,
      border: item.border,
      soft: item.soft,
      text: item.text,
      hasColor: true,
      normalizedLabel: normalizePersianText(label),
    })),
  )
  .sort((left, right) => right.normalizedLabel.length - left.normalizedLabel.length)

export function detectTeamColorVisual(teamName: string): TeamColorVisual {
  const normalizedTeamName = normalizePersianText(teamName ?? '')

  if (!normalizedTeamName) {
    return defaultTeamColorVisual
  }

  for (const item of normalizedTeamColorMatches) {
    let startIndex = normalizedTeamName.indexOf(item.normalizedLabel)

    while (startIndex !== -1) {
      if (hasPhraseBoundary(normalizedTeamName, startIndex, item.normalizedLabel.length)) {
        return {
          key: item.key,
          label: item.label,
          color: item.color,
          border: item.border,
          soft: item.soft,
          text: item.text,
          hasColor: true,
        }
      }

      startIndex = normalizedTeamName.indexOf(item.normalizedLabel, startIndex + 1)
    }
  }

  return defaultTeamColorVisual
}
