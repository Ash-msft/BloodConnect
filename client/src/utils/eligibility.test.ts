import { describe, expect, it } from 'vitest'
import { formatBloodGroup } from '../api/types'
import { getNextEligibleDate, isEligibleToDonate } from './eligibility'

describe('isEligibleToDonate', () => {
  it('is eligible when the donor has never donated', () => {
    expect(isEligibleToDonate(null)).toBe(true)
  })

  it('is eligible exactly at the interval boundary', () => {
    const last = '2026-01-01T00:00:00.000Z'
    const asOf = new Date('2026-02-26T00:00:00.000Z') // +56 days
    expect(isEligibleToDonate(last, asOf, 56)).toBe(true)
  })

  it('is not eligible one day before the interval boundary', () => {
    const last = '2026-01-01T00:00:00.000Z'
    const asOf = new Date('2026-02-25T00:00:00.000Z') // +55 days
    expect(isEligibleToDonate(last, asOf, 56)).toBe(false)
  })

  it('respects a custom configured interval', () => {
    const last = '2026-01-01T00:00:00.000Z'
    const asOf = new Date('2026-01-31T00:00:00.000Z') // +30 days
    expect(isEligibleToDonate(last, asOf, 30)).toBe(true)
    expect(isEligibleToDonate(last, asOf, 31)).toBe(false)
  })
})

describe('getNextEligibleDate', () => {
  it('adds the interval in days to the last donation date', () => {
    const next = getNextEligibleDate('2026-01-01T00:00:00.000Z', 56)
    expect(next.toISOString().slice(0, 10)).toBe('2026-02-26')
  })
})

describe('formatBloodGroup', () => {
  it.each([
    ['OPositive', 'O+'],
    ['ONegative', 'O-'],
    ['APositive', 'A+'],
    ['ANegative', 'A-'],
    ['BPositive', 'B+'],
    ['BNegative', 'B-'],
    ['ABPositive', 'AB+'],
    ['ABNegative', 'AB-'],
  ] as const)('formats %s as %s', (input, expected) => {
    expect(formatBloodGroup(input)).toBe(expected)
  })
})
