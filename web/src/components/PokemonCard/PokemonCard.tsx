import { Link } from "react-router";
import type { PokemonCard as Card } from "../../api/types";
import { dexNumber } from "../../lib/format";
import { cx } from "../styleVars";
import { TypeChip } from "../TypeChip/TypeChip";
import { useGlow } from "../useGlow";
import styles from "./PokemonCard.module.css";

interface PokemonCardProps {
  pokemon: Card;
  selected?: boolean;
}

export function specialClass(pokemon: Pick<Card, "isLegendary" | "isMythical">) {
  if (pokemon.isLegendary) return "legendary";
  if (pokemon.isMythical) return "mythical";
  return null;
}

export function PokemonCard({ pokemon, selected = false }: PokemonCardProps) {
  const glow = useGlow(pokemon.artworkUrl, pokemon.speciesColor);
  const special = specialClass(pokemon);

  return (
    <Link
      to={`/pokemon/${pokemon.id}`}
      className={cx(styles.card, special && styles[special])}
      style={glow}
      aria-current={selected ? "true" : undefined}
    >
      <span className={styles.art}>
        {pokemon.artworkUrl && (
          <img src={pokemon.artworkUrl} alt="" width={280} height={280} loading="lazy" />
        )}
      </span>
      <span className={styles.number}>{dexNumber(pokemon.id)}</span>
      <span className={styles.name}>{pokemon.name}</span>
      <span className={styles.chips}>
        {pokemon.types.map((t) => (
          <TypeChip key={t.name} type={t} />
        ))}
      </span>
      <span className={styles.foot}>
        <i className={styles.hex} aria-hidden="true" />
        <b>{pokemon.total}</b> total
      </span>
      {special && (
        <span className={styles.flag}>{pokemon.isLegendary ? "Legendario" : "Mítico"}</span>
      )}
    </Link>
  );
}
